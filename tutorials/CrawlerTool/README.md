# CrawlerTool — Web Crawler with Content Analysis

A web crawler built with [PocketFlow](../../README.md) that crawls websites and analyses content using a local LLM via Ollama.

## Features

- Crawls websites while staying within the same domain
- Extracts clean text content and links from every page using **HtmlAgilityPack**
- Analyses each page with an LLM (Ollama) to produce:
  - A concise summary (2–3 sentences)
  - Main topics / keywords (up to 5)
  - Content-type classification (article, product page, etc.)
- Processes pages in batches of 5 for efficiency
- Prints a formatted analysis report to the console

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.ai/) running locally at `http://localhost:11434` with a model pulled (default: `gemma3:latest`)

## Usage

```bash
# Interactive — you will be prompted for the URL
dotnet run --project CrawlerTool

# Non-interactive
dotnet run --project CrawlerTool -- --url=https://example.com --max-pages=20
```

### CLI options

| Option | Description | Default |
|---|---|---|
| `--url=<URL>` | Website URL to crawl | *(prompted)* |
| `--max-pages=<N>` | Maximum number of pages to crawl | `10` |

### Environment variables

| Variable | Description | Default |
|---|---|---|
| `OLLAMA_HOST` | Ollama API base URL | `http://localhost:11434` |
| `OLLAMA_MODEL` | LLM model to use | `gemma3:latest` |

## Project structure

```
CrawlerTool/
├── CrawlerTool.csproj          # Project file (references PocketFlow + SharedUtils)
├── Program.cs                  # Entry point — builds flow and runs it
├── CrawlWebsiteNode.cs         # Node: crawl the website
├── AnalyzeContentBatchNode.cs  # BatchNode: analyse pages in batches via LLM
├── GenerateReportNode.cs       # Node: format and print the report
└── README.md                   # This file

SharedUtils/ (shared library)
├── WebCrawlerUtils.cs          # WebCrawler, WebContentAnalyzer, data records
└── OllamaConnector.cs          # LLM / embedding wrapper
```

## PocketFlow pipeline

```
CrawlWebsiteNode
      │ default
      ▼
AnalyzeContentBatchNode
      │ default
      ▼
GenerateReportNode
```

## Output example

```
🕷️  Starting crawler for: https://example.com  (max 5 pages)
Crawling: https://example.com

Report generated:
Analysis Report
Total pages analyzed: 1

Page:         https://example.com
Title:        Example Domain
Summary:      This page serves as an illustrative example domain...
Topics:       example, domain, documentation, placeholder, web
Content Type: informational page
--------------------------------------------------------------------------------
```

