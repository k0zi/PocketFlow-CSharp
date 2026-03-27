using PocketFlow;

namespace ThoughtActionObservation;

/// <summary>
/// ObserveNode – calls the LLM to produce an objective observation about the
/// result of the last action, then stores it in shared["observations"] and
/// loops back to <see cref="ThinkNode"/>.
/// Ported from nodes.py :: ObserveNode.
/// </summary>
public class ObserveNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var action       = store.TryGetValue("current_action",        out var a)  ? (string)a  : "";
        var actionInput  = store.TryGetValue("current_action_input",  out var ai) ? (string)ai : "";
        var actionResult = store.TryGetValue("current_action_result", out var ar) ? ar?.ToString() ?? "" : "";
        return (action, actionInput, actionResult);
    }

    protected override object? Execute(object? prepRes)
    {
        var (action, actionInput, actionResult) = ((string, string, string))prepRes!;

        var prompt = $"""
            You are an observer. Analyze the action result and provide a concise, objective
            observation. Do not make decisions – only describe what you see.

            Action: {action}
            Action input: {actionInput}
            Action result: {actionResult}

            Provide a short observation of this result.
            """;

        var observation = OllamaConnector.CallLlm(prompt);

        var preview = observation.Length > 60
            ? observation[..60] + "..."
            : observation;
        Console.WriteLine($"👁️ Observation: {preview}");

        return observation;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;

        if (!store.ContainsKey("observations"))
            store["observations"] = new List<string>();

        ((List<string>)store["observations"]).Add((string)execRes!);

        return "think";
    }
}

