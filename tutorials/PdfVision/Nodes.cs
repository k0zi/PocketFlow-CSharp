using PocketFlow;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// BatchNode that discovers all PDF files in the project's <c>pdfs</c> directory
/// and processes each one through a dedicated single-PDF flow.
/// C# port of <c>ProcessPDFBatchNode</c> from nodes.py
/// (pocketflow-tool-pdf-vision cookbook).
/// </summary>
class ProcessPdfBatchNode : BatchNode
{
    /// <summary>
    /// Returns one parameter dictionary per PDF file found in the <c>pdfs</c> directory.
    /// </summary>
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var pdfDir = Path.Combine(AppContext.BaseDirectory, "pdfs");

        if (!Directory.Exists(pdfDir))
        {
            Console.WriteLine($"'pdfs' directory not found at: {pdfDir}");
            return new List<Dictionary<string, object>>();
        }

        var defaultPrompt =
            "Extract all text from this document, preserving formatting and layout.";

        var pdfFiles = Directory
            .EnumerateFiles(pdfDir, "*.pdf", SearchOption.TopDirectoryOnly)
            .Concat(Directory.EnumerateFiles(pdfDir, "*.PDF", SearchOption.TopDirectoryOnly))
            .Select(path => new Dictionary<string, object>
            {
                ["pdf_path"]           = path,
                ["extraction_prompt"]  = store.TryGetValue("extraction_prompt", out var ep)
                                            ? ep
                                            : defaultPrompt
            })
            .ToList();

        if (pdfFiles.Count == 0)
            Console.WriteLine("No PDF files found in 'pdfs' directory!");
        else
            Console.WriteLine($"Found {pdfFiles.Count} PDF file(s)");

        return pdfFiles;
    }

    /// <summary>
    /// Processes a single PDF item by running a dedicated single-PDF flow.
    /// Returns a result dictionary with <c>filename</c> and <c>text</c>.
    /// </summary>
    protected override object? Execute(object? prepRes)
    {
        var item    = (Dictionary<string, object>)prepRes!;
        var pdfPath = (string)item["pdf_path"];

        Console.WriteLine($"\nProcessing: {Path.GetFileName(pdfPath)}");
        Console.WriteLine(new string('-', 50));

        // Run the single-PDF sub-flow with a copy of this item as shared state.
        var singleFlow   = FlowFactory.CreateSinglePdfFlow();
        var singleShared = new Dictionary<string, object>(item);
        singleFlow.Run(singleShared);

        return new Dictionary<string, object>
        {
            ["filename"] = Path.GetFileName(pdfPath),
            ["text"]     = singleShared.TryGetValue("final_text", out var t)
                               ? t
                               : "No text extracted"
        };
    }

    /// <summary>
    /// Stores all per-file result dictionaries into <c>shared["results"]</c>.
    /// </summary>
    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["results"] = execRes!;
        return "default";
    }
}

// ── Single-PDF pipeline nodes ────────────────────────────────────────────────

/// <summary>
/// Node that loads a single PDF from <c>shared["pdf_path"]</c> and converts
/// every page to an <c>Image&lt;Rgba32&gt;</c> stored in <c>shared["page_images"]</c>.
/// C# port of <c>LoadPDFNode</c> from nodes.py (pocketflow-tool-pdf-vision cookbook).
/// </summary>
class LoadPdfNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("pdf_path", out var p) ? (string)p : string.Empty;
    }

    protected override object? Execute(object? prepRes)
        => PdfUtils.PdfToImages((string)prepRes!);

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["page_images"] = execRes!;
        return "default";
    }
}

/// <summary>
/// Node that iterates over every page image stored in <c>shared["page_images"]</c>
/// and calls the OpenAI Vision API to extract text.
/// Results are stored in <c>shared["extracted_text"]</c>.
/// C# port of <c>ExtractTextNode</c> from nodes.py (pocketflow-tool-pdf-vision cookbook).
/// </summary>
class ExtractTextNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store   = (Dictionary<string, object>)shared;
        var images  = store.TryGetValue("page_images", out var imgs)
            ? imgs
            : new List<(SixLabors.ImageSharp.Image<Rgba32> Image, int PageNumber)>();
        var prompt  = store.TryGetValue("extraction_prompt", out var pr) ? (string?)pr : null;
        return (images, prompt);
    }

    protected override object? Execute(object? prepRes)
    {
        var (imagesObj, prompt) =
            ((object, string?))prepRes!;

        var images = (List<(SixLabors.ImageSharp.Image<Rgba32> Image, int PageNumber)>)imagesObj;
        var results = new List<Dictionary<string, object>>();

        foreach (var (img, pageNum) in images)
        {
            Console.WriteLine($"  Extracting text from page {pageNum}...");
            var pngBytes = PdfUtils.ImageToPngBytes(img);
            var text     = OpenAiUtils.ExtractTextFromImage(pngBytes, prompt);
            results.Add(new Dictionary<string, object>
            {
                ["page"] = pageNum,
                ["text"] = text
            });
        }

        return results;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["extracted_text"] = execRes!;
        return "default";
    }
}

/// <summary>
/// Node that combines per-page extraction results (from <c>shared["extracted_text"]</c>)
/// into a single formatted string stored in <c>shared["final_text"]</c>.
/// C# port of <c>CombineResultsNode</c> from nodes.py (pocketflow-tool-pdf-vision cookbook).
/// </summary>
class CombineResultsNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("extracted_text", out var et)
            ? et
            : new List<Dictionary<string, object>>();
    }

    protected override object? Execute(object? prepRes)
    {
        var results = (List<Dictionary<string, object>>)prepRes!;
        var parts   = results
            .OrderBy(r => (int)r["page"])
            .Select(r => $"=== Page {r["page"]} ===\n{r["text"]}\n");
        return string.Join("\n", parts);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["final_text"] = execRes!;
        return "default";
    }
}

