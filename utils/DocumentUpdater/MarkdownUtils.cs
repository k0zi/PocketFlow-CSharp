using System.Text.RegularExpressions;

namespace DocumentUpdater;

/// <summary>
/// Utility methods for processing Markdown files into MDC (Cursor rule) format.
/// C# port of the helper functions in <c>utils/update_pocketflow_mdc.py</c>.
/// </summary>
internal static class MarkdownUtils
{
    // ── HTML ─────────────────────────────────────────────────────────────────

    /// <summary>Removes all HTML tags from <paramref name="content"/> using a regex.</summary>
    public static string StripHtmlTags(string content) =>
        Regex.Replace(content, @"<[^>]*>", string.Empty);

    // ── Front-matter ─────────────────────────────────────────────────────────

    /// <summary>
    /// Parses YAML front-matter and returns a dictionary containing any of
    /// <c>title</c>, <c>parent</c>, and <c>nav_order</c> that are present.
    /// </summary>
    public static Dictionary<string, string> ExtractFrontmatter(string filePath)
    {
        var result = new Dictionary<string, string>();
        try
        {
            var content = File.ReadAllText(filePath);
            var m = Regex.Match(content, @"^---\s*(.+?)\s*---", RegexOptions.Singleline);
            if (m.Success)
            {
                var fm       = m.Groups[1].Value;
                var title    = Regex.Match(fm, @"title:\s*""?([^""\n]+)""?");
                var parent   = Regex.Match(fm, @"parent:\s*""?([^""\n]+)""?");
                var navOrder = Regex.Match(fm, @"nav_order:\s*(\d+)");

                if (title.Success)    result["title"]     = title.Groups[1].Value;
                if (parent.Success)   result["parent"]    = parent.Groups[1].Value;
                if (navOrder.Success) result["nav_order"] = navOrder.Groups[1].Value;
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error reading frontmatter from {filePath}: {e.Message}");
        }
        return result;
    }

    /// <summary>
    /// Extracts the first ATX heading (<c># …</c>) from the Markdown body,
    /// falling back to a title-cased version of the filename stem.
    /// </summary>
    public static string ExtractFirstHeading(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            content = Regex.Replace(content, @"^---.*?---\s*", string.Empty, RegexOptions.Singleline);
            var m = Regex.Match(content, @"#\s+(.+)");
            if (m.Success) return m.Groups[1].Value.Trim();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error extracting heading from {filePath}: {e.Message}");
        }

        var stem = Path.GetFileNameWithoutExtension(filePath).Replace('_', ' ');
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(stem.ToLower());
    }

    // ── MDC metadata ─────────────────────────────────────────────────────────

    /// <summary>
    /// Derives the MDC <c>description</c> field from the file's path and parsed metadata.
    /// </summary>
    public static string GetMdcDescription(
        string mdFile,
        Dictionary<string, string> frontmatter,
        string heading)
    {
        var parts = mdFile.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        string section = parts.Contains("core_abstraction") ? "Core Abstraction"
                       : parts.Contains("design_pattern")   ? "Design Pattern"
                       : parts.Contains("utility_function") ? "Utility Function"
                       : string.Empty;

        var fileName = Path.GetFileName(mdFile);
        if (fileName == "guide.md")
            return "Guidelines for using PocketFlow, Agentic Coding";
        if (fileName == "index.md" && string.IsNullOrEmpty(section))
            return "Guidelines for using PocketFlow, a minimalist LLM framework";

        var subsection = frontmatter.TryGetValue("title", out var t) ? t : heading;
        return string.IsNullOrEmpty(section)
            ? $"Guidelines for using PocketFlow, {subsection}"
            : $"Guidelines for using PocketFlow, {section}, {subsection}";
    }

    // ── Content processing ───────────────────────────────────────────────────

    /// <summary>
    /// Removes front-matter and HTML from <paramref name="content"/>, and
    /// optionally rewrites or strips local relative links.
    /// </summary>
    public static string ProcessMarkdownContent(string content, bool removeLocalRefs = false)
    {
        // Remove YAML front-matter
        content = Regex.Replace(content, @"^---.*?---\s*", string.Empty, RegexOptions.Singleline);

        // Drop <div> blocks entirely
        content = Regex.Replace(content, @"<div.*?>.*?</div>", string.Empty, RegexOptions.Singleline);

        if (removeLocalRefs)
        {
            // Keep the link label in brackets, discard the local target
            content = Regex.Replace(content, @"\[([^\]]+)\]\(\./[^)]+\)", "[$1]");
        }
        else
        {
            // Convert relative links to mdc: protocol links
            content = Regex.Replace(content, @"\]\(\./([^)]+)\)",          "](mdc:./$1)");
            content = Regex.Replace(content, @"\]\(mdc:\./(.+?)\.md\)",   "](mdc:./$1.md)");
            content = Regex.Replace(content, @"\]\(mdc:\./(.+?)\.html\)", "](mdc:./$1.md)");
        }

        return StripHtmlTags(content);
    }

    // ── MDC file generation ──────────────────────────────────────────────────

    /// <summary>Returns the DOCUMENTATION FIRST POLICY preamble included in the combined guide.</summary>
    public static string GetDocumentationFirstPolicy() =>
        "# DOCUMENTATION FIRST POLICY\n\n" +
        "**CRITICAL INSTRUCTION**: When implementing a Pocket Flow app:\n\n" +
        "1. **ALWAYS REQUEST MDC FILES FIRST** - Before writing any code, request and review all relevant MDC documentation files. This doc provides an explaination of the documents.\n" +
        "2. **UNDERSTAND THE FRAMEWORK** - Gain comprehensive understanding of the Pocket Flow framework from documentation\n" +
        "3. **AVOID ASSUMPTION-DRIVEN DEVELOPMENT** - Do not base your implementation on assumptions or guesswork. Even if the human didn't explicitly mention pocket flow in their request, if the code you are editing is using pocket flow, you should request relevant docs to help you understand best practice as well before editing.\n\n" +
        "**VERIFICATION**: Begin each implementation with a brief summary of the documentation you've reviewed to inform your approach.\n\n";

    /// <summary>
    /// Generates the YAML front-matter block for an MDC file.
    /// When <paramref name="alwaysApply"/> is <c>true</c> the glob is set to <c>**/*.cs</c>.
    /// </summary>
    public static string GenerateMdcHeader(string description, bool alwaysApply = false)
    {
        var globs = alwaysApply ? "**/*.cs" : string.Empty;
        return $"---\ndescription: {description}\nglobs: {globs}\nalwaysApply: {(alwaysApply ? "true" : "false")}\n---\n";
    }

    // ── Content validation ───────────────────────────────────────────────────

    /// <summary>
    /// Returns <c>true</c> when the content has more than trivial text after
    /// front-matter and whitespace are removed.
    /// </summary>
    public static bool HasSubstantiveContent(string content)
    {
        var stripped = Regex.Replace(content, @"^---.*?---\s*", string.Empty, RegexOptions.Singleline);
        stripped = Regex.Replace(stripped, @"\s+",     string.Empty);
        stripped = Regex.Replace(stripped, @"\{:.*?\}", string.Empty);
        return stripped.Length > 20;
    }
}

