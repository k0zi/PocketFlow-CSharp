using System.Text.RegularExpressions;
using PocketFlow;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SearchTool;

/// <summary>
/// Node to perform a web search using DuckDuckGo (via <see cref="WebSearchUtils"/>).
/// Mirrors nodes.py → SearchNode.
/// </summary>
public class SearchNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("query", out var q) ? (string)q : string.Empty;
    }

    protected override object? Execute(object? prepRes)
    {
        var query = (string)prepRes!;
        if (string.IsNullOrWhiteSpace(query))
            return string.Empty;

        Console.WriteLine($"🔍 Searching for: {query}");
        return WebSearchUtils.SearchWebDuckDuckGo(query);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["search_results"] = execRes ?? string.Empty;
        return "default";
    }
}

/// <summary>
/// Node to analyze search results using the LLM.
/// Mirrors nodes.py → AnalyzeResultsNode and tools/parser.py → analyze_results().
/// LLM calls are delegated to <see cref="OllamaConnector"/> (SharedUtils).
/// </summary>
public class AnalyzeResultsNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store   = (Dictionary<string, object>)shared;
        var query   = store.TryGetValue("query", out var q) ? (string)q : string.Empty;
        var results = store.TryGetValue("search_results", out var r) ? (string)r : string.Empty;
        return (query, results);
    }

    protected override object? Execute(object? prepRes)
    {
        var (query, results) = ((string, string))prepRes!;

        if (string.IsNullOrWhiteSpace(results))
        {
            return new Dictionary<string, object>
            {
                ["summary"]           = "No search results to analyze",
                ["key_points"]        = new List<string>(),
                ["follow_up_queries"] = new List<string>()
            };
        }

        Console.WriteLine("🤖 Analyzing results with LLM...");
        return AnalyzeResults(query, results);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store    = (Dictionary<string, object>)shared;
        var analysis = (Dictionary<string, object>)execRes!;
        store["analysis"] = analysis;

        Console.WriteLine("\n📋 Search Analysis:");
        Console.WriteLine($"\nSummary: {analysis["summary"]}");

        Console.WriteLine("\nKey Points:");
        foreach (var point in (List<string>)analysis["key_points"])
            Console.WriteLine($"  - {point}");

        Console.WriteLine("\nSuggested Follow-up Queries:");
        foreach (var q in (List<string>)analysis["follow_up_queries"])
            Console.WriteLine($"  - {q}");

        return "default";
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static Dictionary<string, object> AnalyzeResults(string query, string results)
    {
        var prompt = $"""
            Analyze these search results for the query: "{query}"

            {results}

            Please provide:
            1. A concise summary of the findings (2-3 sentences)
            2. Key points or facts (up to 5 bullet points)
            3. Suggested follow-up queries (2-3)

            Output in YAML format:
            ```yaml
            summary: |
                brief summary here
            key_points:
                - point 1
                - point 2
            follow_up_queries:
                - query 1
                - query 2
            ```
            IMPORTANT: Always use the | block scalar for summary so colons inside
            the text do not break YAML parsing.
            """;

        var response = OllamaConnector.CallLlm(prompt);
        return ParseYamlAnalysis(response);
    }

    private static Dictionary<string, object> ParseYamlAnalysis(string llmResponse)
    {
        try
        {
            var match = Regex.Match(
                llmResponse,
                @"```yaml(.*?)```",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var yaml = match.Success ? match.Groups[1].Value.Trim() : llmResponse.Trim();

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var raw = deserializer.Deserialize<Dictionary<string, object>>(yaml);

            static List<string> ToStringList(object? obj) =>
                obj is List<object> list
                    ? list.Select(x => x?.ToString() ?? string.Empty).ToList()
                    : new List<string>();

            return new Dictionary<string, object>
            {
                ["summary"]           = raw.TryGetValue("summary", out var s) ? s?.ToString()?.Trim() ?? "" : "",
                ["key_points"]        = ToStringList(raw.TryGetValue("key_points", out var kp) ? kp : null),
                ["follow_up_queries"] = ToStringList(raw.TryGetValue("follow_up_queries", out var fq) ? fq : null)
            };
        }
        catch
        {
            return new Dictionary<string, object>
            {
                ["summary"]           = "Error analyzing results",
                ["key_points"]        = new List<string>(),
                ["follow_up_queries"] = new List<string>()
            };
        }
    }
}

