using PocketFlow;

namespace MCP;

/// <summary>
/// Executes the tool chosen by <see cref="DecideToolNode"/> and prints the result.
/// Mirrors <c>ExecuteToolNode</c> from <c>main.py</c>.
/// </summary>
public class ExecuteToolNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (
            store["tool_name"].ToString()!,
            (Dictionary<string, object?>)store["parameters"]
        );
    }

    protected override object? Execute(object? prepRes)
    {
        var (toolName, parameters) = ((string, Dictionary<string, object?>))prepRes!;
        Console.WriteLine($"🔧 Executing tool '{toolName}' with parameters: " +
                          string.Join(", ", parameters.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        return Utils.CallTool(toolName, parameters);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        Console.WriteLine($"\n✅ Final Answer: {execRes}");
        return "done";
    }
}