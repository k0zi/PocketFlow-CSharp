using System.Diagnostics;
using PocketFlow;
using Parallel;

// ── Load source document ──────────────────────────────────────────────────────

var sourceReadmePath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "README.md");
sourceReadmePath = Path.GetFullPath(sourceReadmePath);

if (!File.Exists(sourceReadmePath))
{
    // Fallback: try relative to working directory (e.g. when running with dotnet run)
    sourceReadmePath = Path.GetFullPath("../../README.md");
}

if (!File.Exists(sourceReadmePath))
{
    Console.Error.WriteLine($"Error: Could not find the source README.md. Tried: {sourceReadmePath}");
    return 1;
}

var text = await File.ReadAllTextAsync(sourceReadmePath);

// ── Shared state ──────────────────────────────────────────────────────────────

var shared = new Dictionary<string, object>
{
    ["text"]       = text,
    ["languages"]  = new List<string> { "Chinese", "Spanish", "Japanese", "German", "Russian", "Portuguese", "French", "Korean" },
    ["output_dir"] = "translations"
};

// ── Build flow ────────────────────────────────────────────────────────────────

var translateNode = new TranslateTextNodeParallel(maxRetries: 3);
var flow          = new AsyncFlow(start: translateNode);

// ── Run ───────────────────────────────────────────────────────────────────────

Console.WriteLine($"Starting parallel translation into {((List<string>)shared["languages"]).Count} languages...");

var sw = Stopwatch.StartNew();
await flow.RunAsync(shared);
sw.Stop();

Console.WriteLine($"\nTotal parallel translation time: {sw.Elapsed.TotalSeconds:F4} seconds");
Console.WriteLine("\n=== Translation Complete ===");
Console.WriteLine($"Translations saved to: {shared["output_dir"]}");
Console.WriteLine("============================");

return 0;
