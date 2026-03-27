using PocketFlow;
using SearchTool;

// ── Build the flow (mirrors flow.py) ─────────────────────────────────────────

var search  = new SearchNode();
var analyze = new AnalyzeResultsNode();

search.Then(analyze);

var flow = new Flow(start: search);

// ── Read query from CLI or interactive prompt (mirrors main.py) ───────────────

var query = string.Empty;
foreach (var arg in args)
{
    if (arg.StartsWith("--"))
    {
        query = arg[2..];
        break;
    }
}

if (string.IsNullOrWhiteSpace(query))
{
    Console.Write("Enter search query: ");
    query = Console.ReadLine() ?? string.Empty;
}

if (string.IsNullOrWhiteSpace(query))
{
    Console.Error.WriteLine("Error: Query is required");
    return;
}

// ── Run ───────────────────────────────────────────────────────────────────────

var shared = new Dictionary<string, object>
{
    ["query"]       = query,
    ["num_results"] = 5
};

Console.WriteLine($"🔎 Search query: {query}");
flow.Run(shared);
