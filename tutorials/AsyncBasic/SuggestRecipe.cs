using PocketFlow;

namespace AsyncBasic;

/// <summary>
/// AsyncNode that asks the LLM to pick the best recipe from the fetched list.
/// Mirrors <c>SuggestRecipe</c> in nodes.py.
/// </summary>
public class SuggestRecipe : AsyncNode
{
    /// <summary>Reads the recipe list from shared state.</summary>
    protected override Task<object?> PrepAsync(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return Task.FromResult<object?>(store["recipes"]);
    }

    /// <summary>Sends the recipe list to the LLM and returns its suggestion.</summary>
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        var recipes = (List<string>)prepRes!;
        var prompt  = $"Choose best recipe from: {string.Join(", ", recipes)}";
        return await OllamaConnector.CallLlmAsync(prompt);
    }

    /// <summary>Stores the suggestion and advances to "approve".</summary>
    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["suggestion"] = (string)execRes!;
        return Task.FromResult<object?>("approve");
    }
}