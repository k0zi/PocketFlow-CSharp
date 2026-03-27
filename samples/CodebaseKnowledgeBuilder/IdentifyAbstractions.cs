using System.Text;
using PocketFlow;

namespace CodebaseKnowledgeBuilder;

public class IdentifyAbstractions : Node
{
    public IdentifyAbstractions(int maxRetries = 1, int wait = 0) : base(maxRetries, wait) { }

    protected override object? Prepare(object shared)
    {
        var store        = (Dictionary<string, object>)shared;
        var files        = (List<(string path, string content)>)store["files"];
        var projectName  = (string)store["project_name"];
        var language     = SharedStore.Get(store, "language", "english");
        var useCache     = SharedStore.Get(store, "use_cache", true);
        var maxAbstrNum  = SharedStore.Get(store, "max_abstraction_num", 10);

        var context = new StringBuilder();
        var fileInfo = new List<(int idx, string path)>();
        for (int i = 0; i < files.Count; i++)
        {
            var (path, content) = files[i];
            context.AppendLine($"--- File Index {i}: {path} ---");
            context.AppendLine(content);
            context.AppendLine();
            fileInfo.Add((i, path));
        }

        var listing = string.Join("\n", fileInfo.Select(f => $"- {f.idx} # {f.path}"));

        return (context.ToString(), listing, files.Count, projectName, language, useCache, maxAbstrNum);
    }

    protected override object? Execute(object? prepRes)
    {
        var (context, listing, fileCount, projectName, language, useCache, maxAbstrNum) =
            ((string, string, int, string, string, bool, int))prepRes!;

        Console.WriteLine("Identifying abstractions using LLM...");

        var langInstr = "";
        var nameLangHint = "";
        var descLangHint = "";
        if (!language.Equals("english", StringComparison.OrdinalIgnoreCase))
        {
            var lc = Capitalize(language);
            langInstr     = $"IMPORTANT: Generate the `name` and `description` for each abstraction in **{lc}** language. Do NOT use English for these fields.\n\n";
            nameLangHint  = $" (value in {lc})";
            descLangHint  = $" (value in {lc})";
        }

        var prompt = $"""
                      For the project `{projectName}`:

                      Codebase Context:
                      {context}

                      {langInstr}Analyze the codebase context.
                      Identify the top 5-{maxAbstrNum} core most important abstractions to help those new to the codebase.

                      For each abstraction, provide:
                      1. A concise `name`{nameLangHint}.
                      2. A beginner-friendly `description` explaining what it is with a simple analogy, in around 100 words{descLangHint}.
                      3. A list of relevant `file_indices` (integers) using the format `idx # path/comment`.

                      List of file indices and paths present in the context:
                      {listing}

                      Format the output as a YAML list of dictionaries:

                      ```yaml
                      - name: |
                          Query Processing{nameLangHint}
                        description: |
                          Explains what the abstraction does.
                          It's like a central dispatcher routing requests.{descLangHint}
                        file_indices:
                          - 0 # path/to/file1.py
                          - 3 # path/to/related.py
                      # ... up to {maxAbstrNum} abstractions
                      ```
                      """;

        var response = LlmCache.Call(prompt, useCache && CurRetry == 0);

        // Validate YAML
        var rawList = YamlHelper.ParseYamlBlock<List<Dictionary<object, object>>>(response);
        if (rawList == null) throw new InvalidOperationException("LLM output is not a list");

        var result = new List<Dictionary<string, object>>();
        foreach (var item in rawList)
        {
            var name  = item["name"]?.ToString()?.Trim()
                        ?? throw new InvalidOperationException($"Missing name in {item}");
            var desc  = item["description"]?.ToString()?.Trim()
                        ?? throw new InvalidOperationException($"Missing description in {item}");
            var idxRaw = item.TryGetValue("file_indices", out var fi) ? fi : null;

            if (idxRaw is not System.Collections.IEnumerable idxList)
                throw new InvalidOperationException($"file_indices is not a list in {name}");

            var indices = new List<int>();
            foreach (var entry in idxList)
            {
                var idx = ParseIndex(entry?.ToString() ?? "", fileCount, name);
                indices.Add(idx);
            }

            result.Add(new Dictionary<string, object>
            {
                ["name"]        = name,
                ["description"] = desc,
                ["files"]       = indices.Distinct().OrderBy(x => x).ToList(),
            });
        }

        Console.WriteLine($"Identified {result.Count} abstractions.");
        return result;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["abstractions"] = execRes!;
        return null;
    }

    private static int ParseIndex(string entry, int count, string name)
    {
        var s = entry.Contains('#') ? entry.Split('#')[0].Trim() : entry.Trim();
        if (!int.TryParse(s, out int idx) || idx < 0 || idx >= count)
            throw new InvalidOperationException($"Invalid file index '{entry}' in '{name}'");
        return idx;
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower();
}