using System.Text.RegularExpressions;
using PocketFlow;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Represents a single work-experience entry extracted from a resume.
/// </summary>
class ExperienceEntry
{
    public string Title   { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
}

/// <summary>
/// All structured data extracted from a resume.
/// </summary>
class ResumeData
{
    public string             Name         { get; set; } = string.Empty;
    public string             Email        { get; set; } = string.Empty;
    public List<ExperienceEntry> Experience { get; set; } = [];
    public List<int>?         SkillIndexes { get; set; }
}

/// <summary>
/// Extracts structured data from resume text using prompt engineering and YAML output.
/// C# port of <c>ResumeParserNode</c> from the pocketflow-structured-output cookbook (main.py).
/// LLM calls are provided by <see cref="OllamaConnector"/> from the shared <c>SharedUtils</c> project.
/// </summary>
class ResumeParserNode : Node
{
    private static readonly IDeserializer YamlDeserializer =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    public ResumeParserNode(int maxRetries = 1, int wait = 0) : base(maxRetries, wait) { }

    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var resumeText   = (string)store["resume_text"];
        var targetSkills = store.TryGetValue("target_skills", out var ts)
            ? (List<string>)ts
            : [];
        return (resumeText, targetSkills);
    }

    protected override object? Execute(object? prepRes)
    {
        var (resumeText, targetSkills) = ((string, List<string>))prepRes!;

        var skillListForPrompt = string.Join("\n",
            targetSkills.Select((skill, i) => $"{i}: {skill}"));

        var prompt = $"""
            Analyze the resume below. Output ONLY the requested information in YAML format.

            **Resume:**
            ```
            {resumeText}
            ```

            **Target Skills (use these indexes):**
            ```
            {skillListForPrompt}
            ```

            **YAML Output Requirements:**
            - Extract `name` (string).
            - Extract `email` (string).
            - Extract `experience` (list of objects with `title` and `company`).
            - Extract `skill_indexes` (list of integers found from the Target Skills list).
            - **Add a YAML comment (`#`) explaining the source BEFORE `name`, `email`, `experience`, each item in `experience`, and `skill_indexes`.**

            **Example Format:**
            ```yaml
            # Found name at top
            name: Jane Doe
            # Found email in contact info
            email: jane@example.com
            # Experience section analysis
            experience:
              # First job listed
              - title: Manager
                company: Corp A
              # Second job listed
              - title: Assistant
                company: Corp B
            # Skills identified from the target list based on resume content
            skill_indexes:
              # Found 0 at top
              - 0
              # Found 2 in experience
              - 2
            ```

            Generate the YAML output now:
            """;

        var response = OllamaConnector.CallLlm(prompt);

        // Extract YAML block between ```yaml ... ```
        var match = Regex.Match(response, @"```yaml(.*?)```",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var yamlBlock = match.Success
            ? match.Groups[1].Value.Trim()
            : response.Trim();

        var result = YamlDeserializer.Deserialize<ResumeData>(yamlBlock)
                     ?? throw new InvalidOperationException("Parsed YAML is null");

        // Basic validation
        if (string.IsNullOrWhiteSpace(result.Name))
            throw new InvalidOperationException("Validation Failed: Missing 'name'");
        if (string.IsNullOrWhiteSpace(result.Email))
            throw new InvalidOperationException("Validation Failed: Missing 'email'");
        if (result.Experience is null)
            throw new InvalidOperationException("Validation Failed: Missing 'experience'");
        if (result.SkillIndexes is not null)
        {
            foreach (var index in result.SkillIndexes)
            {
                if (index < 0)
                    throw new InvalidOperationException(
                        $"Validation Failed: Skill index '{index}' is not a valid non-negative integer");
            }
        }

        return result;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var data  = (ResumeData)execRes!;

        store["structured_data"] = data;

        Console.WriteLine("\n=== STRUCTURED RESUME DATA ===\n");
        Console.WriteLine($"Name:  {data.Name}");
        Console.WriteLine($"Email: {data.Email}");
        Console.WriteLine("\nExperience:");
        foreach (var exp in data.Experience)
            Console.WriteLine($"  - {exp.Title} at {exp.Company}");
        Console.WriteLine("\nSkill Indexes: " +
            (data.SkillIndexes is { Count: > 0 }
                ? string.Join(", ", data.SkillIndexes)
                : "none"));
        Console.WriteLine("\n==============================\n");
        Console.WriteLine("✅ Extracted resume information.");
        return "default";
    }
}

