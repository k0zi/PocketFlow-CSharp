/// <summary>
/// Console I/O helpers for async flows.
/// Ported from AsyncBasic/Utils.cs.
/// </summary>
public static class ConsoleUtils
{
    /// <summary>
    /// Reads a line from the console asynchronously.
    /// Returns the trimmed, lower-cased answer.
    /// </summary>
    public static async Task<string> GetUserInputAsync(string prompt)
    {
        Console.Write(prompt);
        var answer = await Console.In.ReadLineAsync() ?? string.Empty;
        return answer.Trim().ToLowerInvariant();
    }
}

