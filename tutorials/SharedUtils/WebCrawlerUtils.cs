using System.Text.RegularExpressions;
using HtmlAgilityPack;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// ── Data models ───────────────────────────────────────────────────────────────

/// <summary>Represents the raw content extracted from a single web page.</summary>
public record WebPageContent(string Url, string Title, string Text, List<string> Links);

/// <summary>Represents the LLM-produced analysis of a web page's content.</summary>
public record WebPageAnalysis(string Summary, List<string> Topics, string ContentType);

// ── Crawler ───────────────────────────────────────────────────────────────────

/// <summary>
/// Simple domain-scoped web crawler that extracts text and links from pages.
/// Ported from the Python <c>tools/crawler.py</c>.
/// </summary>
public class WebCrawler
{
    private readonly string _baseUrl;
    private readonly int _maxPages;
    private readonly Uri _baseDomain;
    private readonly HashSet<string> _visited = new(StringComparer.OrdinalIgnoreCase);

    private static readonly HttpClient _http = new(new HttpClientHandler
    {
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 5
    })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public WebCrawler(string baseUrl, int maxPages = 10)
    {
        _baseUrl    = baseUrl;
        _maxPages   = maxPages;
        _baseDomain = new Uri(baseUrl);
    }

    /// <summary>Returns <c>true</c> when <paramref name="url"/> belongs to the same host.</summary>
    private bool IsValidUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        return string.Equals(uri.Host, _baseDomain.Host, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Fetches and parses a single page, returning its content.</summary>
    public WebPageContent? ExtractPageContent(string url)
    {
        try
        {
            var html = _http.GetStringAsync(url).GetAwaiter().GetResult();

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Remove script and style elements
            var nodes = doc.DocumentNode.SelectNodes("//script|//style");
            if (nodes != null)
                foreach (var node in nodes.ToList())
                    node.Remove();

            var title = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim() ?? string.Empty;
            var text  = HtmlEntity.DeEntitize(doc.DocumentNode.InnerText)
                                  .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(l => l.Trim())
                                  .Where(l => l.Length > 0)
                                  .Aggregate((a, b) => a + "\n" + b);

            var links = new List<string>();
            var anchors = doc.DocumentNode.SelectNodes("//a[@href]");
            if (anchors != null)
            {
                foreach (var a in anchors)
                {
                    var href = a.GetAttributeValue("href", string.Empty);
                    if (string.IsNullOrWhiteSpace(href)) continue;
                    if (Uri.TryCreate(_baseDomain, href, out var absolute) && IsValidUrl(absolute.ToString()))
                        links.Add(absolute.GetLeftPart(UriPartial.Query));
                }
            }

            return new WebPageContent(url, title, text, links.Distinct().ToList());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error crawling {url}: {ex.Message}");
            return null;
        }
    }

    /// <summary>Crawls the site starting from <see cref="_baseUrl"/>, up to <see cref="_maxPages"/> pages.</summary>
    public List<WebPageContent> Crawl()
    {
        var toVisit = new Queue<string>();
        toVisit.Enqueue(_baseUrl);
        var results = new List<WebPageContent>();

        while (toVisit.Count > 0 && _visited.Count < _maxPages)
        {
            var url = toVisit.Dequeue();
            if (_visited.Contains(url)) continue;

            Console.WriteLine($"Crawling: {url}");
            var content = ExtractPageContent(url);
            if (content is null) continue;

            _visited.Add(url);
            results.Add(content);

            foreach (var link in content.Links)
                if (!_visited.Contains(link) && !toVisit.Contains(link))
                    toVisit.Enqueue(link);
        }

        return results;
    }
}

// ── Content analyser ──────────────────────────────────────────────────────────

/// <summary>
/// Analyses web page content using the LLM and returns structured summaries.
/// Ported from the Python <c>tools/parser.py</c> and <c>utils/call_llm.py</c>.
/// </summary>
public static class WebContentAnalyzer
{
    private static readonly IDeserializer _deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

    /// <summary>Sends page content to the LLM and returns a structured <see cref="WebPageAnalysis"/>.</summary>
    public static WebPageAnalysis AnalyzeContent(WebPageContent content)
    {
        var truncatedText = content.Text.Length > 2000
            ? content.Text[..2000]
            : content.Text;

        var prompt = $"""
                      Analyze this webpage content:

                      Title: {content.Title}
                      URL: {content.Url}
                      Content: {truncatedText}

                      Please provide:
                      1. A brief summary (2-3 sentences)
                      2. Main topics/keywords (up to 5)
                      3. Content type (article, product page, etc.)

                      Output in YAML format:
                      ```yaml
                      summary: >
                          brief summary here
                      topics:
                          - topic 1
                          - topic 2
                      content_type: type here
                      ```
                      """;

        try
        {
            var response = OllamaConnector.CallLlm(prompt);
            var yamlBlock = ExtractYamlBlock(response);

            var parsed = _deserializer.Deserialize<Dictionary<string, object>>(yamlBlock);

            var summary     = parsed.TryGetValue("summary", out var s) ? s?.ToString() ?? string.Empty : string.Empty;
            var contentType = parsed.TryGetValue("content_type", out var ct) ? ct?.ToString() ?? "unknown" : "unknown";
            var topics      = new List<string>();

            if (parsed.TryGetValue("topics", out var t) && t is List<object> topicList)
                topics = topicList.Select(x => x?.ToString() ?? string.Empty).ToList();

            return new WebPageAnalysis(summary, topics, contentType);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error analyzing content: {ex.Message}");
            return new WebPageAnalysis("Error analyzing content", [], "unknown");
        }
    }

    /// <summary>Analyses all pages in <paramref name="pages"/> and attaches analysis results.</summary>
    public static List<(WebPageContent Page, WebPageAnalysis Analysis)> AnalyzeSite(
        IEnumerable<WebPageContent> pages)
    {
        var results = new List<(WebPageContent, WebPageAnalysis)>();
        foreach (var page in pages)
            if (!string.IsNullOrWhiteSpace(page.Text))
                results.Add((page, AnalyzeContent(page)));
        return results;
    }

    // ── YAML helpers ─────────────────────────────────────────────────────────

    private static string ExtractYamlBlock(string text)
    {
        var match = Regex.Match(text, @"```yaml(.*?)```",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }
}


