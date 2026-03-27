using PocketFlow;

namespace Parallel;

/// <summary>
/// Translates a source document into multiple languages in parallel and saves
/// each result to a file.  Mirrors <c>TranslateTextNodeParallel</c> in main.py.
/// </summary>
public class TranslateTextNodeParallel : AsyncParallelBatchNode
{
    public TranslateTextNodeParallel(int maxRetries = 3) : base(maxRetries) { }
    
    /// <summary>
    /// Reads the source text and target languages from the shared store and
    /// returns one (text, language) pair per language as the batch input.
    /// </summary>
    protected override Task<object?> PrepAsync(object shared)
    {
        var store     = (Dictionary<string, object>)shared;
        var text      = store.TryGetValue("text",      out var t) ? (string)t          : "(No text provided)";
        var languages = store.TryGetValue("languages", out var l) ? (List<string>)l    : new List<string>();

        // Each item in the list is passed as prepRes to one ExecAsync call.
        var items = languages
            .Select(lang => (object?)(text, lang))
            .ToList();

        return Task.FromResult<object?>(items);
    }
    
    /// <summary>
    /// Calls the LLM to translate the text into one target language.
    /// Returns a <c>(string Language, string Translation)</c> value tuple.
    /// </summary>
    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        var (text, language) = ((string, string))prepRes!;

        var prompt = $"""
Please translate the following markdown file into {language}.
But keep the original markdown format, links and code blocks.
Directly return the translated text, without any other text or comments.

Original:
{text}

Translated:
""";

        var translation = await OllamaConnector.CallLlmAsync(prompt);
        Console.WriteLine($"Translated {language} text");
        return (language, translation);
    }
    
    /// <summary>
    /// Writes every translated file to <c>output_dir</c> (default:
    /// <c>translations/</c>) as <c>README_LANGUAGE.md</c>.
    /// </summary>
    protected override async Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        var store     = (Dictionary<string, object>)shared;
        var outputDir = store.TryGetValue("output_dir", out var d) ? (string)d : "translations";
        Directory.CreateDirectory(outputDir);

        var results = (List<object?>)execRes!;
        var writeTasks = results
            .OfType<(string Language, string Translation)>()
            .Select(async r =>
            {
                var filename = Path.Combine(outputDir, $"README_{r.Language.ToUpperInvariant()}.md");
                await File.WriteAllTextAsync(filename, r.Translation);
                Console.WriteLine($"Saved translation to {filename}");
            });

        await Task.WhenAll(writeTasks);
        return "default";
    }
}