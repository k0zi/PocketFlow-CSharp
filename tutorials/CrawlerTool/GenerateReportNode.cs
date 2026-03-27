using PocketFlow;

namespace CrawlerTool;

/// <summary>
/// Generates a human-readable analysis report from <c>shared["analyzed_results"]</c>
/// and prints it to the console.
/// Mirrors <c>GenerateReportNode</c> from the Python <c>nodes.py</c>.
/// </summary>
public class GenerateReportNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("analyzed_results", out var r)
            ? (List<(WebPageContent Page, WebPageAnalysis Analysis)>)r
            : new List<(WebPageContent, WebPageAnalysis)>();
    }

    protected override object? Execute(object? prepRes)
    {
        var results = (List<(WebPageContent Page, WebPageAnalysis Analysis)>)prepRes!;

        if (results.Count == 0)
            return "No results to report.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Analysis Report");
        sb.AppendLine($"Total pages analyzed: {results.Count}");

        foreach (var (page, analysis) in results)
        {
            sb.AppendLine();
            sb.AppendLine($"Page:         {page.Url}");
            sb.AppendLine($"Title:        {page.Title}");
            sb.AppendLine($"Summary:      {analysis.Summary}");
            sb.AppendLine($"Topics:       {string.Join(", ", analysis.Topics)}");
            sb.AppendLine($"Content Type: {analysis.ContentType}");
            sb.AppendLine(new string('-', 80));
        }

        return sb.ToString();
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var report = (string)execRes!;
        store["report"] = report;

        Console.WriteLine("\nReport generated:");
        Console.WriteLine(report);
        return "default";
    }
}

