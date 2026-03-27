using PocketFlow;

namespace MCP;

/// <summary>
/// Retrieves the list of available tools and stores them in shared state.
/// Mirrors <c>GetToolsNode</c> from <c>main.py</c>.
/// </summary>
public class GetToolsNode : Node
{
    protected override object? Prepare(object shared)
    {
        Console.WriteLine("🔍 Getting available tools...");
        return null;
    }

    protected override object? Execute(object? prepRes)
        => Utils.GetTools();

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var tools = (List<ToolDefinition>)execRes!;

        store["tools"] = tools;

        // Build a human-readable tool list for the LLM prompt
        var toolInfo = tools.Select((tool, idx) =>
        {
            var paramLines = tool.Properties.Select(kvp =>
            {
                var req = tool.Required.Contains(kvp.Key) ? "(Required)" : "(Optional)";
                return $"    - {kvp.Key} ({kvp.Value.Type}): {req}";
            });
            return $"[{idx + 1}] {tool.Name}\n  Description: {tool.Description}\n  Parameters:\n{string.Join("\n", paramLines)}";
        });

        store["tool_info"] = string.Join("\n", toolInfo);
        return "decide";
    }
}