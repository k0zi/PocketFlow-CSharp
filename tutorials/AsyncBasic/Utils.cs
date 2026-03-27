namespace AsyncBasic;

/// <summary>
/// Utility helpers for the async Recipe Finder.
/// Mirrors utils.py from the original Python example.
/// </summary>
internal static class Utils
{
    // ── Recipe Fetching ──────────────────────────────────────────────────────

    /// <summary>
    /// Fetches mock recipes for the given ingredient asynchronously.
    /// Simulates a remote API call with a short delay.
    /// </summary>
    public static async Task<List<string>> FetchRecipesAsync(string ingredient)
    {
        Console.WriteLine($"Fetching recipes for {ingredient}...");

        // Simulate async I/O (e.g. HTTP request)
        await Task.Delay(1_000);

        var recipes = new List<string>
        {
            $"{ingredient} Stir Fry",
            $"Grilled {ingredient} with Herbs",
            $"Baked {ingredient} with Vegetables"
        };

        Console.WriteLine($"Found {recipes.Count} recipes.");
        return recipes;
    }
}
