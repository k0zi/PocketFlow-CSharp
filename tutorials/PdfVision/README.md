# PdfVision – PDF Text Extraction with OpenAI Vision

This project demonstrates **PDF OCR** using PocketFlow and the OpenAI **gpt-4o Vision API**.  
It is the C# port of the Python [`pocketflow-tool-pdf-vision`](../../cookbook/pocketflow-tool-pdf-vision) cookbook example.

## What This Example Demonstrates

- Converting PDF pages to images for use with a multimodal LLM.
- Extracting text from scanned or image-based PDFs using OpenAI's Vision API.
- Using `BatchNode` to process multiple PDFs from a directory in one run.
- Consolidating shared utilities (`PdfUtils`, `OpenAiUtils`) in the **SharedUtils** project.

## Project Structure

```
PdfVision/
├── PdfVision.csproj        # Project file (references PocketFlow + SharedUtils)
├── Program.cs              # Entry point – wires the flow and runs it
├── Nodes.cs                # PocketFlow nodes (Load, Extract, Combine, BatchProcess)
├── Flow.cs                 # FlowFactory – assembles single-PDF and batch flows
├── README.md               # This file
└── pdfs/                   # Place PDF files here for processing
    └── pocket-flow.pdf     # Sample PDF
```

> PDF rendering (`PdfUtils`) and OpenAI Vision calls (`OpenAiUtils`) live in the
> shared **SharedUtils** project so they can be reused by other examples.

## How It Works

### Single-PDF Flow

```
LoadPdfNode  ──default──▶  ExtractTextNode  ──default──▶  CombineResultsNode
```

| Node                | Responsibility                                                      |
|---------------------|---------------------------------------------------------------------|
| `LoadPdfNode`       | Renders each PDF page to an `Image<Rgba32>` via `PdfUtils`         |
| `ExtractTextNode`   | Sends each page image to OpenAI Vision and collects per-page text  |
| `CombineResultsNode`| Merges all pages into a single, page-ordered text output           |

### Batch Flow

`ProcessPdfBatchNode` (a `BatchNode`) discovers every `*.pdf` in the `pdfs/`
directory, runs the single-PDF flow for each file, and writes all results into
`shared["results"]`.

## Dependencies

| Package / Project      | Purpose                                      |
|------------------------|----------------------------------------------|
| `PocketFlow`           | Flow orchestration framework                 |
| `SharedUtils`          | `PdfUtils` (PDF→image) · `OpenAiUtils` (Vision API) |
| `Docnet.Core`          | Cross-platform PDF-to-image rendering (Pdfium) |
| `OpenAI`               | OpenAI .NET SDK – gpt-4o Vision API          |
| `SixLabors.ImageSharp` | Image encoding (PNG) and pixel manipulation  |

## Setup

1. Set your OpenAI API key:
   ```bash
   export OPENAI_API_KEY=your_api_key_here
   ```
2. Place one or more PDF files in the `pdfs/` directory.

## Usage

```bash
dotnet run
```

### Sample Output

```
PDF Vision – Text Extraction from PDFs
==================================================
Found 1 PDF file(s)

Processing: pocket-flow.pdf
--------------------------------------------------
  Extracting text from page 1...
  Extracting text from page 2...

File: pocket-flow.pdf
--------------------------------------------------
=== Page 1 ===
<extracted text from page 1>

=== Page 2 ===
<extracted text from page 2>
```

## Customisation

Override the extraction prompt at runtime:

```csharp
shared["extraction_prompt"] = "List only the headings found in this document.";
flow.Run(shared);
```

## Limitations

- Maximum rendered page size: **2 000 px** per side (configurable in `PdfUtils.PdfToImages`).
- Requires a valid `OPENAI_API_KEY` environment variable.
- Vision API token limits apply per page image (~1 000 tokens per response by default).

