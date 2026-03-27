using PocketFlow;

namespace CrawlerTool;

/// <summary>
/// Crawls a website starting from the URL stored in <c>shared["base_url"]</c>
/// and writes the page list to <c>shared["crawl_results"]</c>.
/// Mirrors <c>CrawlWebsiteNode</c> from the Python <c>nodes.py</c>.
/// </summary>
public class CrawlWebsiteNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var baseUrl  = store.TryGetValue("base_url", out var u) ? (string)u : string.Empty;
        var maxPages = store.TryGetValue("max_pages", out var m) ? Convert.ToInt32(m) : 10;
        return (baseUrl, maxPages);
    }

    protected override object? Execute(object? prepRes)
    {
        var (baseUrl, maxPages) = ((string, int))prepRes!;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Console.WriteLine("No URL provided.");
            return new List<WebPageContent>();
        }

        var crawler = new WebCrawler(baseUrl, maxPages);
        return crawler.Crawl();
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["crawl_results"] = execRes ?? new List<WebPageContent>();
        return "default";
    }
}

