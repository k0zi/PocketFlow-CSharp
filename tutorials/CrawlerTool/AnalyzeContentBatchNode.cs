using PocketFlow;

namespace CrawlerTool;

/// <summary>
/// Analyses crawled pages in batches of 5 using the LLM and stores the results
/// in <c>shared["analyzed_results"]</c>.
/// Mirrors <c>AnalyzeContentBatchNode</c> from the Python <c>nodes.py</c>.
/// </summary>
public class AnalyzeContentBatchNode : BatchNode
{
    private const int BatchSize = 5;

    protected override object? Prepare(object shared)
    {
        var store   = (Dictionary<string, object>)shared;
        var results = store.TryGetValue("crawl_results", out var r)
            ? (List<WebPageContent>)r
            : new List<WebPageContent>();

        // Split into batches of BatchSize
        var batches = new List<List<WebPageContent>>();
        for (int i = 0; i < results.Count; i += BatchSize)
            batches.Add(results.GetRange(i, Math.Min(BatchSize, results.Count - i)));

        return batches;
    }

    protected override object? Execute(object? prepRes)
    {
        var batch = (List<WebPageContent>)prepRes!;
        return WebContentAnalyzer.AnalyzeSite(batch);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;

        // Flatten results from all batches
        var allResults = new List<(WebPageContent Page, WebPageAnalysis Analysis)>();
        if (execRes is List<object?> batchResults)
        {
            foreach (var batchResult in batchResults)
            {
                if (batchResult is List<(WebPageContent, WebPageAnalysis)> items)
                    allResults.AddRange(items);
            }
        }

        store["analyzed_results"] = allResults;
        return "default";
    }
}

