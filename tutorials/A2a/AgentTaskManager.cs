using System.Runtime.CompilerServices;
using A2aProtocol;
using PocketFlow;

namespace A2a;

/// <summary>
/// Application-level A2A task manager that runs the expense-reimbursement
/// PocketFlow pipeline in response to incoming tasks.
/// Ported from <c>a2a_server.py</c>.
/// </summary>
public sealed class AgentTaskManager : InMemoryTaskManagerBase
{
    private readonly Flow _flow;

    public AgentTaskManager(Flow flow) => _flow = flow;

    // ── Non-streaming send ────────────────────────────────────────────────────

    public override async Task<SendTaskResponse> OnSendTaskAsync(SendTaskRequest request)
    {
        await UpsertTaskAsync(request.Params);
        var task = await UpdateStoreAsync(
            request.Params.Id,
            new A2aTaskStatus { State = TaskState.Working });

        // Run the agent in the background; response is polled by the client
        _ = System.Threading.Tasks.Task.Run(() => RunAgentAsync(request, task));

        return new SendTaskResponse { Id = request.Id, Result = task };
    }

    // ── Streaming (SSE) send ──────────────────────────────────────────────────

    public override async IAsyncEnumerable<SendTaskStreamingResponse> OnSendTaskSubscribeAsync(
        SendTaskRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await UpsertTaskAsync(request.Params);
        var task = await UpdateStoreAsync(
            request.Params.Id,
            new A2aTaskStatus { State = TaskState.Working });

        // Yield initial "working" status
        yield return StatusEvent(request, task, final: false);

        // Run agent and collect result
        Exception? agentError = null;
        A2aTask? completed = null;

        try
        {
            completed = await RunAgentAsync(request, task);
        }
        catch (Exception ex)
        {
            agentError = ex;
        }

        if (agentError is not null)
        {
            var failedTask = await UpdateStoreAsync(
                request.Params.Id,
                new A2aTaskStatus
                {
                    State   = TaskState.Failed,
                    Message = new A2aMessage
                    {
                        Role  = "agent",
                        Parts = [new TextPart { Text = agentError.Message }],
                    },
                });

            yield return StatusEvent(request, failedTask, final: true);
            yield break;
        }

        // Emit artifact events
        if (completed?.Artifacts is { Count: > 0 } artifacts)
        {
            foreach (var artifact in artifacts)
            {
                yield return new SendTaskStreamingResponse
                {
                    Id     = request.Id,
                    Result = new TaskArtifactUpdateEvent
                    {
                        Id       = task.Id,
                        Artifact = artifact,
                    },
                };
            }
        }

        // Final status event
        yield return StatusEvent(request, completed!, final: true);
    }

    // ── Agent Execution ───────────────────────────────────────────────────────

    private async Task<A2aTask> RunAgentAsync(SendTaskRequest request, A2aTask task)
    {
        // Extract text from the first TextPart in the user message
        var textContent = request.Params.Message.Parts
            .OfType<TextPart>()
            .FirstOrDefault()?.Text ?? string.Empty;

        var shared = new Dictionary<string, object> { ["user_message"] = textContent };

        // Run synchronous PocketFlow on a thread-pool thread to avoid blocking
        await System.Threading.Tasks.Task.Run(() => _flow.Run(shared));

        var responseText = shared.TryGetValue("response", out var r)
            ? r.ToString() ?? "I could not process your request."
            : "I could not process your request.";

        var artifact = new Artifact
        {
            Name        = "expense_response",
            Description = "Expense reimbursement response",
            Parts       = [new TextPart { Text = responseText }],
        };

        return await UpdateStoreAsync(
            task.Id,
            new A2aTaskStatus { State = TaskState.Completed },
            [artifact]);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SendTaskStreamingResponse StatusEvent(
        SendTaskRequest request, A2aTask task, bool final)
        => new()
        {
            Id     = request.Id,
            Result = new TaskStatusUpdateEvent
            {
                Id     = task.Id,
                Status = task.Status,
                Final  = final,
            },
        };
}

