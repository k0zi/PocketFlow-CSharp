using System.Diagnostics;
using ParallelFlow;

Console.WriteLine("=== Processing Images in Parallel ===");
Console.WriteLine("Parallel Image Processor");
Console.WriteLine(new string('-', 30));

// ── Discover images ───────────────────────────────────────────────────────────

const string ImagesDir = "images";
if (!Directory.Exists(ImagesDir))
{
    Console.Error.WriteLine($"Error: Directory '{ImagesDir}' not found!");
    return 1;
}

var imagePaths = Directory
    .GetFiles(ImagesDir, "*.jpg")
    .Concat(Directory.GetFiles(ImagesDir, "*.jpeg"))
    .Concat(Directory.GetFiles(ImagesDir, "*.png"))
    .ToList();

if (imagePaths.Count == 0)
{
    Console.Error.WriteLine($"Error: No images found in '{ImagesDir}' directory!");
    return 1;
}

Console.WriteLine($"Found {imagePaths.Count} images:");
foreach (var path in imagePaths)
    Console.WriteLine($"- {path}");

// ── Shared state ──────────────────────────────────────────────────────────────

var shared = new Dictionary<string, object>
{
    ["images"] = imagePaths
};

var (batchFlow, parallelBatchFlow) = FlowFactory.CreateFlows();

// ── Sequential run ────────────────────────────────────────────────────────────

var sw = Stopwatch.StartNew();
Console.WriteLine("\nRunning sequential batch flow...");
await batchFlow.RunAsync(shared);
var batchTime = sw.Elapsed.TotalSeconds;

// ── Parallel run ──────────────────────────────────────────────────────────────

sw.Restart();
Console.WriteLine("\nRunning parallel batch flow...");
await parallelBatchFlow.RunAsync(shared);
var parallelTime = sw.Elapsed.TotalSeconds;

// ── Results ───────────────────────────────────────────────────────────────────

Console.WriteLine("\nTiming Results:");
Console.WriteLine($"Sequential batch processing: {batchTime:F2} seconds");
Console.WriteLine($"Parallel batch processing:   {parallelTime:F2} seconds");
Console.WriteLine($"Speedup: {batchTime / parallelTime:F2}x");
Console.WriteLine("\nProcessing complete! Check the output/ directory for results.");

return 0;

