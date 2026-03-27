using System.Runtime.CompilerServices;

namespace A2aProtocol;

/// <summary>
/// In-memory implementation of <see cref="A2aTaskManagerBase"/>.
/// Provides task storage and helpers that concrete managers can build on.
/// Ported from <c>task_manager.py</c>.
/// </summary>
public abstract class InMemoryTaskManagerBase : A2aTaskManagerBase
{
    private readonly InMemoryCache<A2aTask>                   _tasks     = new();
    private readonly InMemoryCache<TaskPushNotificationConfig> _pushInfos = new();

    // ── Task Store Helpers ────────────────────────────────────────────────────

    /// <summary>
    /// Creates the task if it does not exist yet; returns the stored task.
    /// </summary>
    protected Task<A2aTask> UpsertTaskAsync(TaskSendParams p)
    {
        var task = _tasks.GetOrSet(p.Id, () => new A2aTask
        {
            Id        = p.Id,
            SessionId = p.SessionId,
            Status    = new A2aTaskStatus { State = TaskState.Submitted },
            History   = [p.Message],
        });
        return System.Threading.Tasks.Task.FromResult(task);
    }

    /// <summary>
    /// Updates the status (and optionally appends artifacts) for an existing task.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the task is not found.</exception>
    protected Task<A2aTask> UpdateStoreAsync(
        string        taskId,
        A2aTaskStatus status,
        List<Artifact>? artifacts = null)
    {
        var task = _tasks.Update(taskId, t =>
        {
            t.Status = status;
            if (artifacts is { Count: > 0 })
            {
                t.Artifacts ??= new List<Artifact>();
                t.Artifacts.AddRange(artifacts);
            }
            return t;
        });
        return System.Threading.Tasks.Task.FromResult(task);
    }

    /// <summary>Returns the task or <c>null</c> if it does not exist.</summary>
    protected A2aTask? GetTask(string taskId) => _tasks.Get(taskId);

    /// <summary>Returns a copy of the task with history trimmed to <paramref name="historyLength"/>.</summary>
    protected static A2aTask AppendTaskHistory(A2aTask task, int? historyLength)
    {
        if (historyLength is null || task.History is null)
            return task;

        return new A2aTask
        {
            Id        = task.Id,
            SessionId = task.SessionId,
            Status    = task.Status,
            Artifacts = task.Artifacts,
            Metadata  = task.Metadata,
            History   = task.History.TakeLast(historyLength.Value).ToList(),
        };
    }

    // ── Push-notification Helpers ─────────────────────────────────────────────

    protected void SetPushNotification(TaskPushNotificationConfig config)
        => _pushInfos.Set(config.Id, config);

    protected TaskPushNotificationConfig? GetPushNotification(string taskId)
        => _pushInfos.Get(taskId);

    // ── Default Implementations ───────────────────────────────────────────────

    public override Task<GetTaskResponse> OnGetTaskAsync(GetTaskRequest request)
    {
        var p    = request.Params;
        var task = GetTask(p.Id);

        if (task is null)
            return System.Threading.Tasks.Task.FromResult(
                new GetTaskResponse { Id = request.Id, Error = A2aErrors.TaskNotFound() });

        var result = AppendTaskHistory(task, p.HistoryLength);
        return System.Threading.Tasks.Task.FromResult(
            new GetTaskResponse { Id = request.Id, Result = result });
    }

    public override Task<CancelTaskResponse> OnCancelTaskAsync(CancelTaskRequest request)
    {
        var task = GetTask(request.Params.Id);

        if (task is null)
            return System.Threading.Tasks.Task.FromResult(
                new CancelTaskResponse { Id = request.Id, Error = A2aErrors.TaskNotFound() });

        return System.Threading.Tasks.Task.FromResult(
            new CancelTaskResponse { Id = request.Id, Error = A2aErrors.UnsupportedOperation() });
    }

    public override Task<SetTaskPushNotificationResponse> OnSetTaskPushNotificationAsync(
        SetTaskPushNotificationRequest request)
    {
        var p    = request.Params;
        var task = GetTask(p.Id);

        if (task is null)
            return System.Threading.Tasks.Task.FromResult(
                new SetTaskPushNotificationResponse { Id = request.Id, Error = A2aErrors.TaskNotFound() });

        SetPushNotification(p);
        return System.Threading.Tasks.Task.FromResult(
            new SetTaskPushNotificationResponse { Id = request.Id, Result = p });
    }

    public override Task<GetTaskPushNotificationResponse> OnGetTaskPushNotificationAsync(
        GetTaskPushNotificationRequest request)
    {
        var task         = GetTask(request.Params.Id);
        if (task is null)
            return System.Threading.Tasks.Task.FromResult(
                new GetTaskPushNotificationResponse { Id = request.Id, Error = A2aErrors.TaskNotFound() });

        var notification = GetPushNotification(request.Params.Id);
        if (notification is null)
            return System.Threading.Tasks.Task.FromResult(
                new GetTaskPushNotificationResponse
                    { Id = request.Id, Error = A2aErrors.PushNotificationNotSupported() });

        return System.Threading.Tasks.Task.FromResult(
            new GetTaskPushNotificationResponse { Id = request.Id, Result = notification });
    }

#pragma warning disable CS1998 // Async method lacks 'await' – intentional for async-stream stub
    public override async IAsyncEnumerable<SendTaskStreamingResponse> OnResubscribeToTaskAsync(
        TaskResubscriptionRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new SendTaskStreamingResponse
        {
            Id    = request.Id,
            Error = A2aErrors.UnsupportedOperation(),
        };
    }
#pragma warning restore CS1998
}

