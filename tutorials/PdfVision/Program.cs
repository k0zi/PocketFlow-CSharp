// C# port of main.py + flow.py from the pocketflow-tool-pdf-vision cookbook.
// Batch-processes all PDF files in the 'pdfs' directory using OpenAI gpt-4o Vision.

Console.WriteLine("PDF Vision – Text Extraction from PDFs");
Console.WriteLine(new string('=', 50));

var flow   = FlowFactory.CreateVisionFlow();
var shared = new Dictionary<string, object>();

// Optional: override the extraction prompt.
// shared["extraction_prompt"] = "Extract all text, preserving formatting and layout.";

flow.Run(shared);

// ── Print results ────────────────────────────────────────────────────────────
if (shared.TryGetValue("results", out var resultsObj) &&
    resultsObj is List<object?> results)
{
    foreach (var item in results)
    {
        if (item is not Dictionary<string, object> result) continue;
        Console.WriteLine($"\nFile: {result["filename"]}");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine(result["text"]);
    }
}
