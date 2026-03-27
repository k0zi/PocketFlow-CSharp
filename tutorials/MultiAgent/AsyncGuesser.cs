using PocketFlow;

namespace MultiAgent;

public class AsyncGuesser : AsyncNode
{
    protected override async Task<object?> PrepAsync(object shared)
    {
        var state = (GameState)shared;

        // Block until the hinter sends a hint
        var hint = await state.GuesserChannel.Reader.ReadAsync();
        return (hint, state.PastGuesses.ToList());
    }

    protected override Task<object?> ExecAsync(object? prepRes)
    {
        var (hint, pastGuesses) = ((string, List<string>))prepRes!;

        var prompt = $"Given hint: {hint}";
        if (pastGuesses.Count > 0)
            prompt += $"\nPast wrong guesses: {string.Join(", ", pastGuesses)}";
        prompt += "\nGuess the single word being described. Reply with one word only:";

        var guess = OllamaConnector.CallLlm(prompt).Trim();
        Console.WriteLine($"Guesser: I guess it's - {guess}");
        return Task.FromResult<object?>(guess);
    }

    protected override async Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        var state = (GameState)shared;
        var guess = (string)execRes!;

        if (string.Equals(guess, state.TargetWord, StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Game Over - Correct guess!");
            await state.HinterChannel.Writer.WriteAsync("GAME_OVER");
            return "end";
        }

        state.PastGuesses.Add(guess);
        await state.HinterChannel.Writer.WriteAsync(guess);
        return "continue";
    }
}

