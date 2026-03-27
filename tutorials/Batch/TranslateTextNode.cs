using PocketFlow;

/// <summary>
/// Batch node that translates a markdown document into multiple languages.
/// C# port of TranslateTextNode from the pocketflow-batch cookbook.
/// </summary>
class TranslateTextNode : BatchNode
{
    public TranslateTextNode(int maxRetries = 3) : base(maxRetries) { }

    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var text = store.TryGetValue("text", out var t) ? (string)t : "(No text provided)";
        var languages = store.TryGetValue("languages", out var l)
            ? (List<string>)l
            : ["Chinese", "Spanish", "Japanese", "German", "Russian", "Portuguese", "French", "Korean"];

        // Return a list of (text, language) tuples as the batch items
        return languages.Select(lang => (object)(text, lang)).ToList();
    }

    protected override object? Execute(object? prepRes)
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

        var result = OllamaConnector.CallLlm(prompt);
        Console.WriteLine($"Translated {language} text");
        return (language, result);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var outputDir = store.TryGetValue("output_dir", out var d) ? (string)d : "translations";

        Directory.CreateDirectory(outputDir);

        var results = (List<object?>)execRes!;
        foreach (var item in results)
        {
            var (language, translation) = ((string, string))item!;
            var filename = Path.Combine(outputDir, $"README_{language.ToUpperInvariant()}.md");
            File.WriteAllText(filename, translation, System.Text.Encoding.UTF8);
            Console.WriteLine($"Saved translation to {filename}");
        }

        return null;
    }
}

