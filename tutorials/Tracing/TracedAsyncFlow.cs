using PocketFlow;

namespace Tracing;

/// <summary>
/// Abstract base class for asynchronous PocketFlow flows with automatic Langfuse tracing.
///
/// Inherit from <see cref="TracedAsyncFlow"/> instead of <see cref="AsyncFlow"/> to trace
/// every async node run and the overall flow execution automatically.
/// Equivalent to applying the Python <c>@trace_flow</c> decorator to an <c>AsyncFlow</c>
/// subclass (tracing/decorator.py).
///
/// <example>
/// <code>
/// public class MyAsyncFlow : TracedAsyncFlow
/// {
///     public MyAsyncFlow(TracingConfig? cfg = null)
///         : base(flowName: "MyAsyncFlow", config: cfg)
///     {
///         var fetch   = new FetchNode();
///         var process = new ProcessNode();
///         fetch.On("process").Next(process);
///         StartNode = fetch;
///     }
/// }
/// </code>
/// </example>
/// </summary>
public abstract class TracedAsyncFlow : AsyncFlow
{
    private readonly LangfuseTracer _tracer;
    private readonly string         _flowName;
    private readonly Stack<string>  _spanStack = new();

    protected TracedAsyncFlow(
        BaseNode?      start    = null,
        string?        flowName = null,
        TracingConfig? config   = null)
        : base(start)
    {
        _flowName = flowName ?? GetType().Name;
        _tracer   = new LangfuseTracer(config ?? TracingConfig.FromEnv());
    }

    // ── Flow-level tracing (override RunAsync to wrap entire execution) ───────

    public override async Task<object?> RunAsync(object shared)
    {
        _tracer.StartTrace(_flowName, shared);
        try
        {
            var result = await base.RunAsync(shared);
            _tracer.EndTrace(shared, "success");
            await _tracer.FlushAsync();
            return result;
        }
        catch (Exception)
        {
            _tracer.EndTrace(shared, "error");
            await _tracer.FlushAsync();
            throw;
        }
    }

    // ── Node-level hooks (called from AsyncFlow._OrchAsync) ──────────────────

    protected override Task OnBeforeNodeRunAsync(BaseNode node, object shared)
    {
        var spanKey = _tracer.StartNodeSpan(
            node.GetType().Name,
            Guid.NewGuid().ToString(),
            "run");
        _spanStack.Push(spanKey ?? string.Empty);
        return Task.CompletedTask;
    }

    protected override Task OnAfterNodeRunAsync(BaseNode node, object shared, string? action)
    {
        if (_spanStack.Count == 0) return Task.CompletedTask;
        var key = _spanStack.Pop();
        if (!string.IsNullOrEmpty(key))
            _tracer.EndNodeSpan(key, outputData: action);
        return Task.CompletedTask;
    }

    protected override Task OnNodeErrorAsync(BaseNode node, object shared, Exception error)
    {
        if (_spanStack.Count == 0) return Task.CompletedTask;
        var key = _spanStack.Pop();
        if (!string.IsNullOrEmpty(key))
            _tracer.EndNodeSpan(key, error: error);
        return Task.CompletedTask;
    }
}


