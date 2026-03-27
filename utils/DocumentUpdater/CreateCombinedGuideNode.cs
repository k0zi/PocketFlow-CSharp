using PocketFlow;

namespace DocumentUpdater;

/// <summary>
/// Combines <c>guide.md</c> and <c>index.md</c> from the docs directory into a single
/// <c>guide_for_pocketflow.mdc</c> file prefixed with the Documentation First Policy.
/// Equivalent to <c>create_combined_guide()</c> in <c>update_pocketflow_mdc.py</c>.
/// </summary>
public class CreateCombinedGuideNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store    = (Dictionary<string, object?>)shared;
        var docsDir  = (string)store["docs_dir"]!;
        var rulesDir = (string)store["rules_dir"]!;
        return (docsDir, rulesDir);
    }

    protected override object? Execute(object? prepRes)
    {
        var (docsDir, rulesDir) = ((string, string))prepRes!;

        var guideFile = Path.Combine(docsDir, "guide.md");
        var indexFile = Path.Combine(docsDir, "index.md");

        if (!File.Exists(guideFile) || !File.Exists(indexFile))
        {
            Console.WriteLine("Warning: guide.md or index.md not found — skipping combined guide.");
            return false;
        }

        var guideContent = File.ReadAllText(guideFile);
        var indexContent = File.ReadAllText(indexFile);

        var processedGuide = MarkdownUtils.ProcessMarkdownContent(guideContent, removeLocalRefs: true);
        var processedIndex = MarkdownUtils.ProcessMarkdownContent(indexContent, removeLocalRefs: true);

        var docFirstPolicy = MarkdownUtils.GetDocumentationFirstPolicy();
        var combinedContent = docFirstPolicy + processedGuide + "\n\n" + processedIndex;

        const string description = "Guidelines for using PocketFlow, Agentic Coding";
        var mdcHeader = MarkdownUtils.GenerateMdcHeader(description, alwaysApply: true);
        var mdcContent = mdcHeader + combinedContent;

        var outputPath = Path.Combine(rulesDir, "guide_for_pocketflow.mdc");
        File.WriteAllText(outputPath, mdcContent);

        Console.WriteLine($"Created combined guide MDC file: {outputPath}");
        return true;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes) => null;
}

