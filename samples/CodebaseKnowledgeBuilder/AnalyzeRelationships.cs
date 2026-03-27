using System.Text;
using PocketFlow;

namespace CodebaseKnowledgeBuilder;

public class AnalyzeRelationships : Node
{
    public AnalyzeRelationships(int maxRetries = 1, int wait = 0) : base(maxRetries, wait) { }

    protected override object? Prepare(object shared)
    {
        var store       = (Dictionary<string, object>)shared;
        var abstractions = (List<Dictionary<string, object>>)store["abstractions"];
        var files        = (List<(string path, string content)>)store["files"];
        var projectName  = (string)store["project_name"];
        var language     = SharedStore.Get(store, "language", "english");
        var useCache     = SharedStore.Get(store, "use_cache", true);

        var allIndices   = new HashSet<int>();
        var abstrLines   = new List<string>();
        var ctx          = new StringBuilder("Identified Abstractions:\n");

        for (int i = 0; i < abstractions.Count; i++)
        {
            var a       = abstractions[i];
            var name    = a["name"].ToString()!;
            var desc    = a["description"].ToString()!;
            var fileIds = (List<int>)a["files"];
            ctx.AppendLine($"- Index {i}: {name} (Relevant file indices: [{string.Join(", ", fileIds)}])");
            ctx.AppendLine($"  Description: {desc}");
            abstrLines.Add($"{i} # {name}");
            foreach (var f in fileIds) allIndices.Add(f);
        }

        ctx.AppendLine("\nRelevant File Snippets (Referenced by Index and Path):");
        var contentMap = FileHelper.GetContentForIndices(files, allIndices.OrderBy(x => x));
        foreach (var kv in contentMap)
            ctx.AppendLine($"--- File: {kv.Key} ---\n{kv.Value}");

        return (ctx.ToString(), string.Join("\n", abstrLines),
            abstractions.Count, projectName, language, useCache);
    }

    protected override object? Execute(object? prepRes)
    {
        var (context, listing, numAbstr, projectName, language, useCache) =
            ((string, string, int, string, string, bool))prepRes!;

        Console.WriteLine("Analyzing relationships using LLM...");

        var langInstr = "";
        var langHint  = "";
        var listNote  = "";
        if (!language.Equals("english", StringComparison.OrdinalIgnoreCase))
        {
            var lc     = Capitalize(language);
            langInstr  = $"IMPORTANT: Generate the `summary` and relationship `label` fields in **{lc}** language. Do NOT use English for these fields.\n\n";
            langHint   = $" (in {lc})";
            listNote   = $" (Names might be in {lc})";
        }

        var prompt = $"""
                      Based on the following abstractions and relevant code snippets from the project `{projectName}`:

                      List of Abstraction Indices and Names{listNote}:
                      {listing}

                      Context (Abstractions, Descriptions, Code):
                      {context}

                      {langInstr}Please provide:
                      1. A high-level `summary` of the project's main purpose and functionality in a few beginner-friendly sentences{langHint}. Use markdown formatting with **bold** and *italic* text to highlight important concepts.
                      2. A list (`relationships`) describing the key interactions between these abstractions. For each relationship, specify:
                          - `from_abstraction`: Index of the source abstraction (e.g., `0 # AbstractionName1`)
                          - `to_abstraction`: Index of the target abstraction (e.g., `1 # AbstractionName2`)
                          - `label`: A brief label for the interaction **in just a few words**{langHint} (e.g., "Manages", "Inherits", "Uses").
                          Simplify the relationship and exclude those non-important ones.

                      IMPORTANT: Make sure EVERY abstraction is involved in at least ONE relationship (either as source or target).

                      Format the output as YAML:

                      ```yaml
                      summary: |
                        A brief, simple explanation of the project{langHint}.
                      relationships:
                        - from_abstraction: 0 # AbstractionName1
                          to_abstraction: 1 # AbstractionName2
                          label: "Manages"{langHint}
                      ```

                      Now, provide the YAML output:
                      """;

        var response = LlmCache.Call(prompt, useCache && CurRetry == 0);
        var parsed   = YamlHelper.ParseYamlBlock<Dictionary<object, object>>(response);

        var summary = parsed["summary"]?.ToString()?.Trim()
                      ?? throw new InvalidOperationException("Missing 'summary' in LLM output");

        if (parsed["relationships"] is not System.Collections.IEnumerable relList)
            throw new InvalidOperationException("'relationships' is not a list");

        var details = new List<Dictionary<string, object>>();
        foreach (var rel in relList.Cast<Dictionary<object, object>>())
        {
            var fromIdx = ParseRelIndex(rel["from_abstraction"]?.ToString() ?? "", numAbstr);
            var toIdx   = ParseRelIndex(rel["to_abstraction"]?.ToString() ?? "", numAbstr);
            var label   = rel["label"]?.ToString()?.Trim()
                          ?? throw new InvalidOperationException("Missing 'label' in relationship");
            details.Add(new() { ["from"] = fromIdx, ["to"] = toIdx, ["label"] = label });
        }

        Console.WriteLine("Generated project summary and relationship details.");
        return new Dictionary<string, object> { ["summary"] = summary, ["details"] = details };
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["relationships"] = execRes!;
        return null;
    }

    private static int ParseRelIndex(string entry, int count)
    {
        var s = entry.Contains('#') ? entry.Split('#')[0].Trim() : entry.Trim();
        if (!int.TryParse(s, out int idx) || idx < 0 || idx >= count)
            throw new InvalidOperationException($"Invalid relationship index '{entry}'");
        return idx;
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower();
}