using System.Text.RegularExpressions;
using PocketFlow;

namespace CodebaseKnowledgeBuilder;

public class WriteChapters : BatchNode
{
    public WriteChapters(int maxRetries = 1, int wait = 0) : base(maxRetries, wait) { }

    // Progressive context across batch items (cleared in Post)
    private List<string> _chaptersWrittenSoFar = new();

    protected override object? Prepare(object shared)
    {
        var store        = (Dictionary<string, object>)shared;
        var chapterOrder = (List<int>)store["chapter_order"];
        var abstractions = (List<Dictionary<string, object>>)store["abstractions"];
        var files        = (List<(string path, string content)>)store["files"];
        var projectName  = (string)store["project_name"];
        var language     = SharedStore.Get(store, "language", "english");
        var useCache     = SharedStore.Get(store, "use_cache", true);

        _chaptersWrittenSoFar = new();

        // Build chapter filename lookup
        var chapterFilenames = new Dictionary<int, Dictionary<string, object>>();
        var allChapters      = new List<string>();

        for (int i = 0; i < chapterOrder.Count; i++)
        {
            int abstrIdx = chapterOrder[i];
            if (abstrIdx < 0 || abstrIdx >= abstractions.Count) continue;
            var name     = abstractions[abstrIdx]["name"].ToString()!;
            var safeName = Regex.Replace(name, @"[^\w]", "_").ToLowerInvariant();
            var filename = $"{i + 1:D2}_{safeName}.md";
            chapterFilenames[abstrIdx] = new()
            {
                ["num"]      = i + 1,
                ["name"]     = name,
                ["filename"] = filename,
            };
            allChapters.Add($"{i + 1}. [{name}]({filename})");
        }

        var fullListing = string.Join("\n", allChapters);
        var items = new List<Dictionary<string, object>>();

        for (int i = 0; i < chapterOrder.Count; i++)
        {
            int abstrIdx = chapterOrder[i];
            if (abstrIdx < 0 || abstrIdx >= abstractions.Count) continue;
            var abstr      = abstractions[abstrIdx];
            var fileIds    = (List<int>)abstr["files"];
            var contentMap = FileHelper.GetContentForIndices(files, fileIds);

            var prev = i > 0 ? chapterFilenames[chapterOrder[i - 1]] : null;
            var next = i < chapterOrder.Count - 1 ? chapterFilenames[chapterOrder[i + 1]] : null;

            items.Add(new()
            {
                ["chapter_num"]               = i + 1,
                ["abstraction_index"]         = abstrIdx,
                ["abstraction_details"]       = abstr,
                ["related_files_content_map"] = contentMap,
                ["project_name"]              = projectName,
                ["full_chapter_listing"]      = fullListing,
                ["chapter_filenames"]         = chapterFilenames,
                ["prev_chapter"]              = prev!,
                ["next_chapter"]              = next!,
                ["language"]                  = language,
                ["use_cache"]                 = useCache,
            });
        }

        Console.WriteLine($"Preparing to write {items.Count} chapters...");
        return items;
    }

    protected override object? Execute(object? prepRes)
    {
        var item        = (Dictionary<string, object>)prepRes!;
        var abstr       = (Dictionary<string, object>)item["abstraction_details"];
        var name        = abstr["name"].ToString()!;
        var description = abstr["description"].ToString()!;
        int chapterNum  = (int)item["chapter_num"];
        var projectName = item["project_name"].ToString()!;
        var language    = item["language"].ToString()!;
        bool useCache   = (bool)item["use_cache"];
        var contentMap  = (Dictionary<string, string>)item["related_files_content_map"];
        var fullListing = item["full_chapter_listing"].ToString()!;
        var prev        = item["prev_chapter"] as Dictionary<string, object>;
        var next        = item["next_chapter"] as Dictionary<string, object>;

        Console.WriteLine($"Writing chapter {chapterNum} for: {name} using LLM...");

        var fileCtx = string.Join("\n\n", contentMap.Select(kv =>
        {
            var fname = kv.Key.Contains("# ") ? kv.Key.Split("# ")[1] : kv.Key;
            return $"--- File: {fname} ---\n{kv.Value}";
        }));

        var prevSummary = string.Join("\n---\n", _chaptersWrittenSoFar);

        var langInstr = "";
        var instrHint = "";
        var mermaidHint = "";
        var codeHint = "";
        var linkHint = "";
        var toneHint = "";
        if (!language.Equals("english", StringComparison.OrdinalIgnoreCase))
        {
            var lc        = Capitalize(language);
            langInstr     = $"IMPORTANT: Write this ENTIRE tutorial chapter in **{lc}**. Translate ALL generated content including explanations, examples, technical terms into {lc}. DO NOT use English except in code syntax or required proper nouns. The entire output MUST be in {lc}.\n\n";
            instrHint     = $" (in {lc})";
            mermaidHint   = $" (Use {lc} for labels/text if appropriate)";
            codeHint      = $" (Translate to {lc} if possible)";
            linkHint      = $" (Use the {lc} chapter title from the structure above)";
            toneHint      = $" (appropriate for {lc} readers)";
        }

        var prompt = langInstr + $"""
                      Write a very beginner-friendly tutorial chapter (in Markdown format) for the project `{projectName}` about the concept: "{name}". This is Chapter {chapterNum}.

                      Concept Details:
                      - Name: {name}
                      - Description:
                      {description}

                      Complete Tutorial Structure:
                      {fullListing}

                      Context from previous chapters:
                      {(string.IsNullOrEmpty(prevSummary) ? "This is the first chapter." : prevSummary)}

                      Relevant Code Snippets:
                      {(string.IsNullOrEmpty(fileCtx) ? "No specific code snippets provided for this abstraction." : fileCtx)}

                      Instructions for the chapter:
                      - Start with a clear heading (e.g., `# Chapter {chapterNum}: {name}`).
                      - If not the first chapter, begin with a brief transition from the previous chapter{instrHint}.
                      - Begin with a high-level motivation explaining what problem this abstraction solves{instrHint}.
                      - If complex, break it down into key concepts{instrHint}.
                      - Explain how to use this abstraction with example inputs and outputs{instrHint}.
                      - Each code block should be BELOW 10 lines! Break larger blocks into smaller pieces{instrHint}.
                      - Describe the internal implementation{instrHint}. Use a sequenceDiagram with at most 5 participants{mermaidHint}.
                      - Use mermaid diagrams to illustrate complex concepts{mermaidHint}.
                      - When referring to other abstractions, ALWAYS use proper Markdown links{linkHint}.
                      - Heavily use analogies and examples throughout{instrHint}.
                      - End with a brief conclusion and transition to the next chapter{instrHint}.
                      - Ensure the tone is welcoming and easy for a newcomer{toneHint}.
                      - Output *only* the Markdown content for this chapter.

                      Now, directly provide a super beginner-friendly Markdown output (DON'T need ```markdown``` tags):
                      """;

        var content = LlmCache.Call(prompt, useCache && CurRetry == 0);

        // Ensure correct heading
        var expectedHeading = $"# Chapter {chapterNum}: {name}";
        if (!content.TrimStart().StartsWith($"# Chapter {chapterNum}"))
        {
            var lines = content.TrimStart().Split('\n').ToList();
            if (lines.Count > 0 && lines[0].TrimStart().StartsWith('#'))
                lines[0] = expectedHeading;
            else
                lines.Insert(0, expectedHeading + "\n");
            content = string.Join('\n', lines);
        }

        _chaptersWrittenSoFar.Add(content);
        return content;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store    = (Dictionary<string, object>)shared;
        var chapters = (List<object?>)execRes!;
        store["chapters"] = chapters.Select(c => c?.ToString() ?? "").ToList();
        _chaptersWrittenSoFar = new();
        Console.WriteLine($"Finished writing {chapters.Count} chapters.");
        return null;
    }

    private static string Capitalize(string s) =>
        string.IsNullOrEmpty(s) ? s : char.ToUpper(s[0]) + s[1..].ToLower();
}