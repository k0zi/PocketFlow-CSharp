using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PocketFlow;

public class Flow : BaseNode
{
    public BaseNode? StartNode { get; set; }

    public Flow(BaseNode? start = null)
    {
        StartNode = start;
    }

    public BaseNode Start(BaseNode start)
    {
        StartNode = start;
        return start;
    }

    public virtual BaseNode? GetNextNode(BaseNode curr, string? action)
    {
        if (curr.Successors.TryGetValue(action ?? "default", out var next))
            return next;
        if (curr.Successors.Count > 0)
            Console.WriteLine($"Warning: Flow ends: '{action}' not found in {string.Join(", ", curr.Successors.Keys)}");
        return null;
    }
    
    protected virtual void OnFlowStarting(object shared) { }
    protected virtual void OnFlowCompleted(object shared, string? result) { }
    protected virtual void OnFlowError(object shared, Exception error) { }
    protected virtual void OnBeforeNodeRun(BaseNode node, object shared) { }
    protected virtual void OnAfterNodeRun(BaseNode node, object shared, string? action) { }
    protected virtual void OnNodeError(BaseNode node, object shared, Exception error) { }

    internal virtual string? _Orch(object shared, Dictionary<string, object>? @params = null)
    {
        if (StartNode == null) return null;
        var curr = (BaseNode)StartNode.Clone();
        var p = @params ?? new Dictionary<string, object>(Params);
        string? lastAction = null;
        while (curr != null)
        {
            curr.SetParams(p);
            OnBeforeNodeRun(curr, shared);
            try
            {
                lastAction = curr.InternalRun(shared)?.ToString();
                OnAfterNodeRun(curr, shared, lastAction);
            }
            catch (Exception e)
            {
                OnNodeError(curr, shared, e);
                throw;
            }
            var next = GetNextNode(curr, lastAction);
            curr = next != null ? (BaseNode)next.Clone() : null;
        }
        return lastAction;
    }

    internal override object? InternalRun(object shared)
    {
        OnFlowStarting(shared);
        try
        {
            var p = Prepare(shared);
            var o = _Orch(shared);
            var result = Post(shared, p, o);
            OnFlowCompleted(shared, o?.ToString());
            return result;
        }
        catch (Exception e)
        {
            OnFlowError(shared, e);
            throw;
        }
    }

    protected override object? Post(object shared, object? prepRes, object? execRes) => execRes;
}