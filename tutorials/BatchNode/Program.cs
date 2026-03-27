using PocketFlow;
using System.Diagnostics;

// C# port of main.py + flow.py from the pocketflow-batch-node cookbook.
// Demonstrates BatchNode by processing a large CSV file in chunks.

const string dataDir = "data";
const string csvPath  = "data/sales.csv";

Directory.CreateDirectory(dataDir);

if (!File.Exists(csvPath))
{
    Console.WriteLine("Creating sample sales.csv...");
    GenerateSampleCsv(csvPath, rows: 10_000, seed: 42);
}

// --- Shared store ---
var shared = new Dictionary<string, object>
{
    ["input_file"] = csvPath,
};

Console.WriteLine("Processing sales.csv in chunks...");
var sw = Stopwatch.StartNew();

// --- Build flow (mirrors flow.py create_flow()) ---
var processor = new CsvProcessorNode(chunkSize: 1_000);
var showStats = new ShowStatsNode();

processor.On("show_stats").Then(showStats);

var flow = new Flow(start: processor);
flow.Run(shared);

sw.Stop();
Console.WriteLine($"Total processing time: {sw.Elapsed.TotalSeconds:F4} seconds");

// ─────────────────────────────────────────────────────────────────────────────
// Helpers
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Generates a sample sales CSV file with the given number of rows.
/// Mirrors the pandas/numpy generation in main.py (mean=100, stdDev=30).
/// </summary>
static void GenerateSampleCsv(string path, int rows, int seed)
{
    var rng       = new Random(seed);
    var products  = new[] { "A", "B", "C" };
    var startDate = new DateOnly(2024, 1, 1);

    using var writer = new StreamWriter(path, false, System.Text.Encoding.UTF8);
    writer.WriteLine("date,amount,product");

    for (int i = 0; i < rows; i++)
    {
        var date    = startDate.AddDays(i);
        var amount  = Math.Round(NormalSample(rng, mean: 100, stdDev: 30), 2);
        var product = products[rng.Next(products.Length)];
        writer.WriteLine($"{date:yyyy-MM-dd},{amount},{product}");
    }
}

/// <summary>Box-Muller normal-distribution sample.</summary>
static double NormalSample(Random rng, double mean, double stdDev)
{
    double u1 = 1.0 - rng.NextDouble();
    double u2 = 1.0 - rng.NextDouble();
    double z  = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
    return mean + stdDev * z;
}
