using PocketFlow;

namespace DocumentUpdater;

/// <summary>
/// Scans the docs directory for all Markdown files and populates the shared store.
/// Also ensures the output (rules) directory exists.
/// Equivalent to the file-discovery logic at the start of <c>generate_mdc_files()</c>
/// in <c>update_pocketflow_mdc.py</c>.
/// </summary>
public class DiscoverFilesNode : Node
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

        if (!Directory.Exists(docsDir))
            throw new DirectoryNotFoundException($"Docs directory not found: {docsDir}");

        Console.WriteLine($"Generating MDC files from docs in : {docsDir}");
        Console.WriteLine($"Output will be written to         : {rulesDir}");

        // Ensure the output directory exists
        Directory.CreateDirectory(rulesDir);

        // Collect all *.md files, excluding guide.md and index.md
        // (those are combined into a single guide file by CreateCombinedGuideNode)
        var mdFiles = Directory
            .EnumerateFiles(docsDir, "*.md", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f) != "index.md" &&
                        Path.GetFileName(f) != "guide.md")
            .OrderBy(f => f)
            .ToList();

        Console.WriteLine($"Discovered {mdFiles.Count} Markdown file(s) to convert.");
        return mdFiles;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object?>)shared;
        store["md_files"] = execRes;   // List<string>
        return null;
    }
}

