using PocketFlow;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// BatchNode that solves a reasoning problem by running multiple independent
/// LLM attempts and returning the most frequent answer (majority vote).
/// C# port of MajorityVoteNode from the pocketflow-majority-vote cookbook.
/// </summary>
class MajorityVoteNode : BatchNode
{
    private static readonly IDeserializer YamlDeserializer =
        new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    public MajorityVoteNode(int maxRetries = 3) : base(maxRetries) { }

    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var question = store.TryGetValue("question", out var q) ? (string)q : "(No question provided)";
        var numTries = store.TryGetValue("num_tries", out var n) ? Convert.ToInt32(n) : 3;

        // Return the same question repeated numTries times as the batch items
        return Enumerable.Repeat((object)question, numTries).ToList();
    }

    protected override object? Execute(object? prepRes)
    {
        var question = (string)prepRes!;

        var prompt = $"""
            You are a helpful assistant. Please answer the user's question below.
            Question: {question}

            Return strictly using the following YAML structure:
            ```yaml
            thinking: |
                (Your thinking process here)
            answer: 0.123 # Final answer as a decimal with 3 decimal places
            ```
            """;

        var rawResponse = OllamaConnector.CallLlm(prompt);

        // Extract YAML block between ```yaml ... ```
        var match = Regex.Match(rawResponse, @"```yaml(.*?)```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        var yamlBlock = match.Success
            ? match.Groups[1].Value.Trim()
            : rawResponse.Trim();

        var parsed = YamlDeserializer.Deserialize<Dictionary<string, object>>(yamlBlock);

        if (parsed == null || !parsed.ContainsKey("answer"))
            throw new InvalidOperationException($"Missing 'answer' in YAML: {yamlBlock}");

        return parsed["answer"]?.ToString() ?? string.Empty;
    }

    protected override object? ExecFallback(object? prepRes, Exception exc)
    {
        Console.Error.WriteLine($"[MajorityVoteNode] attempt failed: {exc.Message}");
        return null;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var results = ((List<object?>)execRes!)
            .Where(r => r is not null)
            .Select(r => r!.ToString()!)
            .ToList();

        if (results.Count == 0)
        {
            Console.WriteLine("No valid answers were collected.");
            return "end";
        }

        // Tally votes
        var counter = results
            .GroupBy(r => r)
            .OrderByDescending(g => g.Count())
            .First();

        var bestAnswer = counter.Key;
        var freq = counter.Count();

        store["majority_answer"] = bestAnswer;

        Console.WriteLine("========================");
        Console.WriteLine($"All structured answers: [{string.Join(", ", results.Select(r => $"'{r}'"))}]");
        Console.WriteLine($"Majority vote => {bestAnswer}");
        Console.WriteLine($"Frequency => {freq}");
        Console.WriteLine("========================");

        return "end";
    }
}


