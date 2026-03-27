using PocketFlow;

namespace ThoughtActionObservation;

/// <summary>
/// EndNode – terminal node that prints a completion message.
/// Ported from nodes.py :: EndNode.
/// </summary>
public class EndNode : Node
{
    protected override object? Prepare(object shared) => null;

    protected override object? Execute(object? prepRes)
    {
        Console.WriteLine("Flow ended, thank you for using!");
        return null;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes) => null;
}

