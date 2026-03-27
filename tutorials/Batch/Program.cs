using PocketFlow;
using System.Diagnostics;

// C# port of main.py from the pocketflow-batch cookbook.
// Translates a markdown document into multiple languages using a BatchNode.

var readmePath = Path.Combine("..", "..", "README.md");
if (!File.Exists(readmePath))
{
    Console.Error.WriteLine($"README.md not found at: {Path.GetFullPath(readmePath)}");
    return;
}

var text = File.ReadAllText(readmePath);

var shared = new Dictionary<string, object>
{
    ["text"]       = text,
    ["languages"]  = new List<string> { "Chinese", "Spanish", "Japanese", "German", "Russian", "Portuguese", "French", "Korean" },
    ["output_dir"] = "translations"
};

var languageCount = ((List<string>)shared["languages"]).Count;
Console.WriteLine($"Starting sequential translation into {languageCount} languages...");

var sw = Stopwatch.StartNew();

var translateNode = new TranslateTextNode(maxRetries: 3);
var flow = new Flow(start: translateNode);
flow.Run(shared);

sw.Stop();

Console.WriteLine($"\nTotal sequential translation time: {sw.Elapsed.TotalSeconds:F4} seconds");
Console.WriteLine("\n=== Translation Complete ===");
Console.WriteLine($"Translations saved to: {shared["output_dir"]}");
Console.WriteLine("============================");

