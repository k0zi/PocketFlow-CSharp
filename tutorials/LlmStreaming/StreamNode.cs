using PocketFlow;

/// <summary>
/// Streams an LLM response token-by-token with optional user-interrupt support.
/// Mirrors <c>StreamNode</c> from the Python pocketflow-llm-streaming example.
/// </summary>
public class StreamNode : AsyncNode
{
    // ── Prep: resolve prompt and create a cancellation source ────────────────

    protected override Task<object?> PrepAsync(object shared)
    {
        var store  = (Dictionary<string, object>)shared;
        var prompt = store.TryGetValue("prompt", out var p) ? (string)p : "What's the meaning of life?";
        var cts    = new CancellationTokenSource();
        return Task.FromResult<object?>((prompt, cts));
    }

    // ── Exec: stream tokens, printing each one; honour interrupt ─────────────

    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        var (prompt, cts) = ((string prompt, CancellationTokenSource cts))prepRes!;

        // Start a background task that waits for the user to press ENTER,
        // then signals cancellation (mirrors Python's interrupt_event thread).
        var listenerTask = Task.Run(async () =>
        {
            try   { await Console.In.ReadLineAsync(cts.Token); cts.Cancel(); }
            catch (OperationCanceledException) { /* streaming finished first */ }
        });

        Console.WriteLine("Press ENTER at any time to interrupt streaming...\n");

        try
        {
            // Swap FakeStreamLlmAsync → StreamLlmAsync to use a real Ollama model.
            await foreach (var token in OllamaConnector.FakeStreamLlmAsync(prompt, cancellationToken: cts.Token))
            {
                Console.Write(token);
                await Task.Delay(100, cts.Token); // simulate per-token display latency
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nUser interrupted streaming.");
        }

        Console.WriteLine();
        return (cts, listenerTask);
    }

    // ── Post: cancel the listener so it doesn't linger ───────────────────────

    protected override async Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        var (cts, listenerTask) = ((CancellationTokenSource cts, Task listenerTask))execRes!;
        cts.Cancel(); // unblock ReadLineAsync if streaming completed naturally
        try   { await listenerTask; }
        catch { /* suppress cancellation noise */ }
        cts.Dispose();
        return "default";
    }
}

