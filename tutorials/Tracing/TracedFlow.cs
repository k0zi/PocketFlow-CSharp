using PocketFlow;

namespace Tracing;

/// <summary>
/// Abstract base class for synchronous PocketFlow flows with automatic Langfuse tracing.
///
/// Inherit from <see cref="TracedFlow"/> instead of <see cref="Flow"/> to trace every
/// node run and the overall flow execution automatically.  Equivalent to applying the
/// Python <c>@trace_flow</c> decorator (tracing/decorator.py).
///
/// <example>
/// <code>
/// public class MyFlow : TracedFlow
/// {
///     public MyFlow(TracingConfig? cfg = null)
///         : base(flowName: "MyFlow", config: cfg)
///     {
///         var a = new NodeA();
///         var b = new NodeB();
///         a.Then(b);
///         StartNode = a;
///     }
/// }
/// </code>
/// </example>
/// </summary>
public abstract class TracedFlow : Flow
{
    private readonly LangfuseTracer _tracer;
    private readonly string         _flowName;
    private readonly Stack<string>  _spanStack = new();

    protected TracedFlow(
        BaseNode?      start    = null,
        string?        flowName = null,
        TracingConfig? config   = null)
        : base(start)
    {
        _flowName = flowName ?? GetType().Name;
        _tracer   = new LangfuseTracer(config ?? TracingConfig.FromEnv());
    }

    // ── Flow-level hooks ─────────────────────────────────────────────────────

    protected override void OnFlowStarting(object shared)
        => _tracer.StartTrace(_flowName, shared);

    protected override void OnFlowCompleted(object shared, string? result)
    {
        _tracer.EndTrace(shared, "success");
        _tracer.Flush();
    }

    protected override void OnFlowError(object shared, Exception error)
    {
        _tracer.EndTrace(shared, "error");
        _tracer.Flush();
    }

    // ── Node-level hooks ─────────────────────────────────────────────────────

    protected override void OnBeforeNodeRun(BaseNode node, object shared)
    {
        var spanKey = _tracer.StartNodeSpan(
            node.GetType().Name,
            Guid.NewGuid().ToString(),
            "run");
        _spanStack.Push(spanKey ?? string.Empty);
    }

    protected override void OnAfterNodeRun(BaseNode node, object shared, string? action)
    {
        if (_spanStack.Count == 0) return;
        var key = _spanStack.Pop();
        if (!string.IsNullOrEmpty(key))
            _tracer.EndNodeSpan(key, outputData: action);
    }

    protected override void OnNodeError(BaseNode node, object shared, Exception error)
    {
        if (_spanStack.Count == 0) return;
        var key = _spanStack.Pop();
        if (!string.IsNullOrEmpty(key))
            _tracer.EndNodeSpan(key, error: error);
    }
}

