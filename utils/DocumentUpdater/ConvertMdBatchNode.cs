using PocketFlow;

namespace DocumentUpdater;

/// <summary>
/// BatchNode that converts every discovered Markdown file into an MDC (Cursor rule) file.
/// <c>Prepare</c> returns the list of file tasks; <c>Execute</c> is called once per file;
/// <c>Post</c> tallies successes and failures.
/// Equivalent to calling <c>convert_md_to_mdc()</c> for each file in
/// <c>generate_mdc_files()</c> (<c>update_pocketflow_mdc.py</c>).
/// </summary>
public class ConvertMdBatchNode : BatchNode
{
    // ── Per-file task ─────────────────────────────────────────────────────────

    private record FileTask(string MdFile, string DocsDir, string RulesDir);

    // ── BatchNode overrides ───────────────────────────────────────────────────

    /// <summary>
    /// Returns a <see cref="List{T}"/> of <see cref="FileTask"/> records —
    /// one per Markdown file discovered by <see cref="DiscoverFilesNode"/>.
    /// </summary>
    protected override object? Prepare(object shared)
    {
        var store    = (Dictionary<string, object?>)shared;
        var mdFiles  = (List<string>)store["md_files"]!;
        var docsDir  = (string)store["docs_dir"]!;
        var rulesDir = (string)store["rules_dir"]!;

        return mdFiles
            .Select(f => (object)new FileTask(f, docsDir, rulesDir))
            .ToList();
    }

    /// <summary>
    /// Converts a single Markdown file to MDC format.
    /// Returns <c>true</c> on success, <c>false</c> on error, <c>null</c> when skipped.
    /// </summary>
    protected override object? Execute(object? prepRes)
    {
        var task = (FileTask)prepRes!;
        return ConvertFile(task.MdFile, task.DocsDir, task.RulesDir);
    }

    /// <summary>Counts successes/failures and prints a summary.</summary>
    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var results      = (List<object?>)execRes!;
        int successCount = results.Count(r => r is true);
        int failureCount = results.Count(r => r is false);

        var store = (Dictionary<string, object?>)shared;
        store["success_count"] = successCount;
        store["failure_count"] = failureCount;

        // +1 for the combined guide created by CreateCombinedGuideNode
        Console.WriteLine($"\nProcessed {results.Count + 1} markdown file(s):");
        Console.WriteLine($"  - Successfully converted : {successCount + 1}");
        Console.WriteLine($"  - Failed conversions     : {failureCount}");

        return null;
    }

    // ── Conversion logic ──────────────────────────────────────────────────────

    /// <summary>
    /// Converts a single <c>.md</c> file to <c>.mdc</c> format in <paramref name="rulesDir"/>,
    /// preserving the relative directory structure from <paramref name="docsDir"/>.
    /// </summary>
    private static bool? ConvertFile(string mdFile, string docsDir, string rulesDir)
    {
        try
        {
            Console.WriteLine($"Processing: {mdFile}");

            var fileName = Path.GetFileName(mdFile);

            // Defensive guard — these are handled by CreateCombinedGuideNode
            if (fileName is "guide.md" or "index.md")
            {
                Console.WriteLine($"  Skipping {fileName} — included in combined guide.");
                return null;
            }

            // Skip empty subfolder index files (e.g. core_abstraction/index.md)
            var parentDir = Path.GetFileName(Path.GetDirectoryName(mdFile)) ?? string.Empty;
            if (fileName == "index.md" &&
                parentDir is "core_abstraction" or "design_pattern" or "utility_function")
            {
                if (!MarkdownUtils.HasSubstantiveContent(File.ReadAllText(mdFile)))
                {
                    Console.WriteLine($"  Skipping empty subfolder index: {mdFile}");
                    return null;
                }
            }

            // Extract metadata
            var frontmatter = MarkdownUtils.ExtractFrontmatter(mdFile);
            var heading     = MarkdownUtils.ExtractFirstHeading(mdFile);
            var description = MarkdownUtils.GetMdcDescription(mdFile, frontmatter, heading);

            // Process content
            var rawContent       = File.ReadAllText(mdFile);
            var processedContent = MarkdownUtils.ProcessMarkdownContent(rawContent);

            if (!MarkdownUtils.HasSubstantiveContent(processedContent))
            {
                Console.WriteLine($"  Skipping file with no substantive content: {mdFile}");
                return null;
            }

            // Build MDC output
            var mdcContent = MarkdownUtils.GenerateMdcHeader(description) + processedContent;

            // Compute output path, stripping leading "docs/" segment if present
            var relPath = Path.GetRelativePath(docsDir, mdFile);
            var parts   = relPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Length > 1 && parts[0].Equals("docs", StringComparison.OrdinalIgnoreCase))
                relPath = Path.Combine(parts.Skip(1).ToArray());

            var outputPath = Path.ChangeExtension(Path.Combine(rulesDir, relPath), ".mdc");
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            File.WriteAllText(outputPath, mdcContent);

            Console.WriteLine($"  Created: {outputPath}");
            return true;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error converting {mdFile}: {e.Message}");
            return false;
        }
    }
}

