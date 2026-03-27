using PocketFlow;

namespace AsyncBasic;

/// <summary>
/// AsyncNode that prompts for an ingredient and fetches matching recipes.
/// Mirrors <c>FetchRecipes</c> in nodes.py.
/// </summary>
public class FetchRecipes : AsyncNode
{
    /// <summary>Prompts the user for an ingredient.</summary>
    protected override async Task<object?> PrepAsync(object shared)
    {
        var ingredient = await ConsoleUtils.GetUserInputAsync("Enter ingredient: ");
        return ingredient;
    }

    /// <summary>Fetches recipes for the ingredient asynchronously.</summary>
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        var ingredient = (string)prepRes!;
        return await Utils.FetchRecipesAsync(ingredient);
    }

    /// <summary>Stores results in shared state and advances to "suggest".</summary>
    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["recipes"]    = (List<string>)execRes!;
        store["ingredient"] = (string)prepRes!;
        return Task.FromResult<object?>("suggest");
    }
}