using PocketFlow;

namespace AsyncBasic;

/// <summary>
/// AsyncNode that asks the user whether to accept the suggested recipe.
/// Returns "accept" or "retry" to control flow.
/// Mirrors <c>GetApproval</c> in nodes.py.
/// </summary>
public class GetApproval : AsyncNode
{
    /// <summary>Reads the current suggestion from shared state.</summary>
    protected override Task<object?> PrepAsync(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return Task.FromResult<object?>(store["suggestion"]);
    }

    /// <summary>Prompts the user for a yes/no answer.</summary>
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        return await ConsoleUtils.GetUserInputAsync("\nAccept this recipe? (y/n): ");
    }

    /// <summary>Routes to "accept" or "retry" based on the user's answer.</summary>
    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        var store  = (Dictionary<string, object>)shared;
        var answer = (string)execRes!;

        if (answer == "y")
        {
            Console.WriteLine("\nGreat choice! Here's your recipe...");
            Console.WriteLine($"Recipe:     {store["suggestion"]}");
            Console.WriteLine($"Ingredient: {store["ingredient"]}");
            return Task.FromResult<object?>("accept");
        }

        Console.WriteLine("\nLet's try another recipe...");
        return Task.FromResult<object?>("retry");
    }
}

