using CrawlerTool;
using PocketFlow;

// ── Build the flow (mirrors flow.py) ─────────────────────────────────────────

var crawl    = new CrawlWebsiteNode();
var analyze  = new AnalyzeContentBatchNode();
var report   = new GenerateReportNode();

crawl.Then(analyze).Then(report);

var flow = new Flow(start: crawl);

// ── Read URL from CLI or prompt (mirrors main.py) ─────────────────────────────

string url;

var urlArg = args.FirstOrDefault(a => a.StartsWith("--url="));
if (urlArg is not null)
{
    url = urlArg["--url=".Length..];
}
else
{
    Console.Write("Enter website URL to crawl (e.g., https://example.com): ");
    url = Console.ReadLine()?.Trim() ?? string.Empty;
}

if (string.IsNullOrWhiteSpace(url))
{
    Console.Error.WriteLine("Error: URL is required.");
    return 1;
}

var maxPages = 10;
var maxArg = args.FirstOrDefault(a => a.StartsWith("--max-pages="));
if (maxArg is not null && int.TryParse(maxArg["--max-pages=".Length..], out var parsed))
    maxPages = parsed;

// ── Run ───────────────────────────────────────────────────────────────────────

var shared = new Dictionary<string, object>
{
    ["base_url"]  = url,
    ["max_pages"] = maxPages
};

Console.WriteLine($"🕷️  Starting crawler for: {url}  (max {maxPages} pages)");
flow.Run(shared);

return 0;
