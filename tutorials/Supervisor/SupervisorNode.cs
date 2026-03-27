using PocketFlow;

namespace Supervisor;

/// <summary>
/// Quality-control node that checks the agent's answer for obvious nonsense.
/// Returns "retry" to restart the inner agent flow when the answer is invalid.
/// Ported from <c>SupervisorNode</c> in nodes.py.
/// </summary>
public class SupervisorNode : Node
{
    private static readonly string[] NonsenseMarkers =
    [
        "coffee break",
        "purple unicorns",
        "made up",
        "42",
        "Who knows?"
    ];

    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("answer", out var a) ? (string?)a : null;
    }

    protected override object? Execute(object? prepRes)
    {
        var answer = prepRes as string ?? string.Empty;

        Console.WriteLine("    🔍 Supervisor checking answer quality...");

        var isNonsense = NonsenseMarkers.Any(marker =>
            answer.Contains(marker, StringComparison.Ordinal));

        return isNonsense
            ? new { Valid = false, Reason = "Answer appears to be nonsensical or unhelpful" }
            : new { Valid = true,  Reason = "Answer appears to be legitimate" };
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store  = (Dictionary<string, object>)shared;
        dynamic result = execRes!;

        if (result.Valid)
        {
            Console.WriteLine($"    ✅ Supervisor approved answer: {result.Reason}");
            return null; // flow ends — answer accepted
        }

        Console.WriteLine($"    ❌ Supervisor rejected answer: {result.Reason}");

        // Clean up and annotate context so the inner flow retries with knowledge
        store["answer"] = string.Empty;
        var context = store.TryGetValue("context", out var c) ? (string)c : string.Empty;
        store["context"] = context + "\n\nNOTE: Previous answer attempt was rejected by supervisor.";

        return "retry";
    }
}

