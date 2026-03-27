using PocketFlow;

namespace ThoughtActionObservation;

/// <summary>
/// ActionNode – executes the action decided by <see cref="ThinkNode"/>.
/// Supports "search" (DuckDuckGo via SharedUtils), "calculate", and
/// "answer" action types.  Stores the result in shared["current_action_result"]
/// and transitions to <see cref="ObserveNode"/>.
/// Ported from nodes.py :: ActionNode.
/// </summary>
public class ActionNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var action      = store.TryGetValue("current_action",       out var a)  ? (string)a  : "answer";
        var actionInput = store.TryGetValue("current_action_input", out var ai) ? (string)ai : "";
        return (action, actionInput);
    }

    protected override object? Execute(object? prepRes)
    {
        var (action, actionInput) = ((string, string))prepRes!;

        Console.WriteLine($"🚀 Executing action: '{action}', input: {actionInput}");

        return action switch
        {
            "search"    => SearchWeb(actionInput),
            "calculate" => Calculate(actionInput),
            "answer"    => actionInput,
            _           => $"Unknown action type: {action}"
        };
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["current_action_result"] = execRes ?? string.Empty;
        Console.WriteLine("✅ Action completed, result obtained");
        return "observe";
    }

    // ── Tool implementations ─────────────────────────────────────────────────

    private static string SearchWeb(string query)
        => WebSearchUtils.SearchWebDuckDuckGo(query);

    private static string Calculate(string expression)
    {
        // Simple numeric expression evaluator via DataTable
        try
        {
            var result = new System.Data.DataTable().Compute(expression, null);
            return $"Calculation result: {result}";
        }
        catch
        {
            return $"Unable to calculate expression: {expression}";
        }
    }
}

