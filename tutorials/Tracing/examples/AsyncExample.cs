using PocketFlow;
using Tracing;

namespace Tracing.Examples;

// ── Nodes ─────────────────────────────────────────────────────────────────────

/// <summary>
/// An async node that simulates fetching data from a remote source.
/// Ported from examples/async_example.py → <c>AsyncDataFetchNode</c>.
/// </summary>
file class AsyncDataFetchNode : AsyncNode
{
    protected override Task<object?> PrepAsync(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        object? query = store.TryGetValue("query", out var q) ? q : "default";
        return Task.FromResult<object?>(query);
    }

    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        var query = prepRes?.ToString() ?? "default";
        Console.WriteLine($"🔍 Fetching data for query: {query}");
        await Task.Delay(100); // simulate network latency

        return new Dictionary<string, object>
        {
            ["query"]     = query,
            ["results"]   = Enumerable.Range(0, 3)
                                      .Select(i => $"Result {i} for {query}")
                                      .ToList<object>(),
            ["timestamp"] = DateTime.UtcNow.ToString("O"),
        };
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["fetched_data"] = execRes!;
        return Task.FromResult<object?>("process");
    }
}

/// <summary>
/// An async node that processes the data fetched by <see cref="AsyncDataFetchNode"/>.
/// Ported from examples/async_example.py → <c>AsyncDataProcessNode</c>.
/// </summary>
file class AsyncDataProcessNode : AsyncNode
{
    protected override Task<object?> PrepAsync(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        object? data  = store.TryGetValue("fetched_data", out var d) ? d : new Dictionary<string, object>();
        return Task.FromResult<object?>(data);
    }

    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("⚙️  Processing fetched data...");
        await Task.Delay(50); // simulate processing

        var data    = (Dictionary<string, object>)prepRes!;
        var results = ((IEnumerable<object>)data["results"])
                          .Select(r => $"PROCESSED: {r}")
                          .ToList<object>();

        return new Dictionary<string, object>
        {
            ["original_query"]    = data["query"],
            ["processed_results"] = results,
            ["result_count"]      = results.Count,
        };
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["processed_data"] = execRes!;
        return Task.FromResult<object?>("default");
    }
}

// ── Traced flow ───────────────────────────────────────────────────────────────

/// <summary>
/// An async flow that fetches then processes data.
/// Inheriting from <see cref="TracedAsyncFlow"/> wires Langfuse tracing automatically.
/// Ported from examples/async_example.py → <c>AsyncDataProcessingFlow</c>.
/// </summary>
file class AsyncDataProcessingFlow : TracedAsyncFlow
{
    public AsyncDataProcessingFlow(TracingConfig? config = null)
        : base(flowName: "AsyncDataProcessingFlow", config: config)
    {
        var fetch   = new AsyncDataFetchNode();
        var process = new AsyncDataProcessNode();
        fetch.On("process").Then(process);
        StartNode = fetch;
    }
}

// ── Entry-point ───────────────────────────────────────────────────────────────

/// <summary>
/// Runner for the asynchronous tracing example.
/// Ported from examples/async_example.py → <c>main()</c>.
/// </summary>
public static class AsyncExample
{
    public static async Task RunAsync(TracingConfig? config = null)
    {
        Console.WriteLine("🚀 Starting PocketFlow Async Tracing Example");
        Console.WriteLine(new string('=', 50));

        var flow   = new AsyncDataProcessingFlow(config);
        var shared = new Dictionary<string, object>
            { ["query"] = "machine learning tutorials" };

        Console.WriteLine($"📥 Input query: {shared["query"]}");

        try
        {
            var result = await flow.RunAsync(shared);
            Console.WriteLine($"🎯 Result action: {result}");
            Console.WriteLine("✅ Async flow completed successfully!");

            if (shared.TryGetValue("processed_data", out var raw))
            {
                var pd = (Dictionary<string, object>)raw;
                Console.WriteLine(
                    $"🎉 Processed {pd["result_count"]} results " +
                    $"for query: {pd["original_query"]}");

                foreach (var r in (IEnumerable<object>)pd["processed_results"])
                    Console.WriteLine($"   - {r}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Async flow failed: {e.Message}");
            throw;
        }

        var host = Environment.GetEnvironmentVariable("LANGFUSE_HOST") ?? "your-langfuse-host";
        Console.WriteLine($"\n📊 Check your Langfuse dashboard: {host}");
    }
}
