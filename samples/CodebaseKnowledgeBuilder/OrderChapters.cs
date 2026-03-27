using System.Text;
using PocketFlow;

namespace CodebaseKnowledgeBuilder;

public class OrderChapters : Node
{
    public OrderChapters(int maxRetries = 1, int wait = 0) : base(maxRetries, wait) { }

    protected override object? Prepare(object shared)
    {
        var store        = (Dictionary<string, object>)shared;
        var abstractions = (List<Dictionary<string, object>>)store["abstractions"];
        var relationships = (Dictionary<string, object>)store["relationships"];
        var projectName  = (string)store["project_name"];
        var language     = SharedStore.Get(store, "language", "english");
        var useCache     = SharedStore.Get(store, "use_cache", true);

        var listing  = string.Join("\n", abstractions.Select((a, i) => $"- {i} # {a["name"]}"));
        var summary  = relationships["summary"].ToString();
        var details  = (List<Dictionary<string, object>>)relationships["details"];

        var ctx = new StringBuilder($"Project Summary:\n{summary}\n\nRelationships:\n");
        foreach (var rel in details)
        {
            var from  = abstractions[(int)rel["from"]]["name"];
            var to    = abstractions[(int)rel["to"]]["name"];
            var label = rel["label"];
            ctx.AppendLine($"- From {rel["from"]} ({from}) to {rel["to"]} ({to}): {label}");
        }

        var listNote = "";
        if (!language.Equals("english", StringComparison.OrdinalIgnoreCase))
            listNote = $" (Names might be in {Capitalize(language)})";

        return (listing, ctx.ToString(), abstractions.Count, projectName, listNote, useCache);
    }

    protected override object? Execute(object? prepRes)
    {
        var (listing, context, numAbstr, projectName, listNote, useCache) =
            ((string, string, int, string, string, bool))prepRes!;

        Console.WriteLine("Determining chapter order using LLM...");

        var prompt = $"""
                      Given the following project abstractions and their relationships for the project `{projectName}`:

                      Abstractions (Index # Name){listNote}:
                      {listing}

                      Context about relationships and project summary:
                      {context}

                      What is the best order to explain these abstractions, from first to last?
                      Explain foundational/user-facing concepts first, then lower-level implementation details.

                      Output the ordered list of abstraction indices, including the name in a comment.

                      ```yaml
                      - 2 # FoundationalConcept
                      - 0 # CoreClassA
                      - 1 # CoreClassB
                      ```

                      Now, provide the YAML output:
                      """;

        var response = LlmCache.Call(prompt, useCache && CurRetry == 0);
        var raw      = YamlHelper.ParseYamlBlock<List<object>>(response);
        if (raw == null) throw new InvalidOperationException("LLM output is not a list");

        var ordered  = new List<int>();
        var seen     = new HashSet<int>();
        foreach (var entry in raw)
        {
            var s   = entry.ToString()!;
            var idx = s.Contains('#') ? int.Parse(s.Split('#')[0].Trim()) : int.Parse(s.Trim());
            if (idx < 0 || idx >= numAbstr) throw new InvalidOperationException($"Invalid index {idx}");
            if (seen.Contains(idx)) throw new InvalidOperationException($"Duplicate index {idx}");
            ordered.Add(idx);
            seen.Add(idx);
        }

        if (ordered.Count != numAbstr)
            throw new InvalidOperationException($"Ordered list has {ordered.Count} items, expected {numAbstr}");

        Console.WriteLine($"Determined chapter order: [{string.Join(", ", ordered)}]");
        return ordered;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["chapter_order"] = execRes!;
        return null;
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower();
}