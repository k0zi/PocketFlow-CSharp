using PocketFlow;

/// <summary>
/// Factory for creating the PDF Vision flows.
/// C# port of flow.py and the <c>create_single_pdf_flow</c> helper in nodes.py
/// (pocketflow-tool-pdf-vision cookbook).
/// </summary>
static class FlowFactory
{
    /// <summary>
    /// Creates the top-level batch flow that processes all PDFs in the
    /// <c>pdfs</c> directory.
    /// </summary>
    public static Flow CreateVisionFlow()
        => new(start: new ProcessPdfBatchNode());

    /// <summary>
    /// Creates a flow for processing a single PDF:
    /// <c>LoadPdfNode</c> → <c>ExtractTextNode</c> → <c>CombineResultsNode</c>.
    /// </summary>
    public static Flow CreateSinglePdfFlow()
    {
        var loadPdf        = new LoadPdfNode();
        var extractText    = new ExtractTextNode();
        var combineResults = new CombineResultsNode();

        loadPdf.Then(extractText).Then(combineResults);

        return new Flow(start: loadPdf);
    }
}

