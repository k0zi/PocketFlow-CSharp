using PocketFlow;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Holds the LLM evaluation result for a single resume candidate.
/// </summary>
class ResumeEvaluation
{
    public string CandidateName { get; set; } = string.Empty;
    public bool Qualifies { get; set; }
    public List<string> Reasons { get; set; } = [];
}

/// <summary>
/// Batch processing phase: evaluates each resume individually via the LLM to determine
/// if the candidate qualifies for an advanced technical role.
/// C# port of <c>EvaluateResumesNode</c> from the pocketflow-map-reduce cookbook (nodes.py).
/// LLM calls are provided by <see cref="OllamaConnector"/> from the shared <c>SharedUtils</c> project.
/// </summary>
class EvaluateResumesNode : BatchNode
{
    private static readonly IDeserializer YamlDeserializer =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    public EvaluateResumesNode(int maxRetries = 3) : base(maxRetries) { }

    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var resumes = (Dictionary<string, string>)store["resumes"];

        // Return a list of (filename, content) tuples as batch items
        return resumes.Select(kv => (object)(kv.Key, kv.Value)).ToList();
    }

    protected override object? Execute(object? prepRes)
    {
        var (filename, content) = ((string, string))prepRes!;

        var prompt = $"""
            Evaluate the following resume and determine if the candidate qualifies for an advanced technical role.
            Criteria for qualification:
            - At least a bachelor's degree in a relevant field
            - At least 3 years of relevant work experience
            - Strong technical skills relevant to the position

            Resume:
            {content}

            Return your evaluation in YAML format:
            ```yaml
            candidate_name: [Name of the candidate]
            qualifies: [true/false]
            reasons:
              - [First reason for qualification/disqualification]
              - [Second reason, if applicable]
            ```
            """;

        var response = OllamaConnector.CallLlm(prompt);

        // Extract YAML block between ```yaml ... ```
        var match = Regex.Match(response, @"```yaml(.*?)```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var yamlBlock = match.Success
            ? match.Groups[1].Value.Trim()
            : response.Trim();

        var evaluation = YamlDeserializer.Deserialize<ResumeEvaluation>(yamlBlock)
                         ?? throw new InvalidOperationException($"Failed to parse YAML for {filename}");

        Console.WriteLine($"Evaluated: {filename} → {evaluation.CandidateName} ({(evaluation.Qualifies ? "Qualifies" : "Does not qualify")})");
        return (filename, evaluation);
    }

    protected override object? ExecFallback(object? prepRes, Exception exc)
    {
        var (filename, _) = ((string, string))prepRes!;
        Console.Error.WriteLine($"[EvaluateResumesNode] Evaluation failed for {filename}: {exc.Message}");
        return (filename, new ResumeEvaluation { CandidateName = "Unknown", Qualifies = false });
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var results = (List<object?>)execRes!;

        store["evaluations"] = results
            .Where(r => r is not null)
            .Select(r => ((string, ResumeEvaluation))r!)
            .ToDictionary(t => t.Item1, t => t.Item2);

        return "default";
    }
}

