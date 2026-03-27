using System.Threading.Channels;

namespace MultiAgent;

/// <summary>
/// Shared game state passed through both agent flows.
/// </summary>
public record class GameState
{
    public required string TargetWord { get; init; }
    public required List<string> ForbiddenWords { get; init; }

    /// <summary>Hinter reads guesses / "GAME_OVER" from this channel.</summary>
    public required Channel<string> HinterChannel { get; init; }

    /// <summary>Guesser reads hints from this channel.</summary>
    public required Channel<string> GuesserChannel { get; init; }

    /// <summary>Accumulates wrong guesses across turns (mutable).</summary>
    public List<string> PastGuesses { get; set; } = [];
}