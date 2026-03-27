# Batch Translation Process

This project demonstrates a batch processing implementation that enables LLMs to translate documents into multiple languages. It is designed to efficiently handle the translation of markdown files while preserving their original formatting.

## Features

- Translates markdown content into multiple languages sequentially via `BatchNode`
- Preserves original markdown structure, links, and code blocks
- Saves translated files to a configurable output directory
- LLM calls are provided by the shared **SharedUtils** project (`OllamaConnector`)

## Getting Started

1. Ensure [Ollama](https://ollama.com/) is running locally (default: `http://localhost:11434`) with a suitable model (default: `gemma3:latest`).

2. Build and run:
   ```bash
   dotnet run --project Batch
   ```

   Optional environment variables:
   | Variable | Default | Description |
   |---|---|---|
   | `OLLAMA_HOST` | `http://localhost:11434` | Ollama server URL |
   | `OLLAMA_MODEL` | `gemma3:latest` | Model to use for translation |

## How It Works

```mermaid
flowchart LR
    batch[TranslateTextNode]
```

`TranslateTextNode` extends `BatchNode` and processes each language translation as a separate batch item:

1. **Prepare** – reads the source document and list of target languages from the shared store; returns a list of `(text, language)` tuples.
2. **Execute** – called once per tuple; sends a translation prompt to the LLM and returns `(language, translatedText)`.
3. **Post** – iterates over all results and writes each translated document to `translations/README_{LANGUAGE}.md`.

## Project Structure

| File | Description |
|---|---|
| `Program.cs` | Entry point – reads `README.md`, starts the timed flow |
| `TranslateTextNode.cs` | `BatchNode` implementation for multi-language translation |
| `Batch.csproj` | Project file with references to **PocketFlow** and **SharedUtils** |

LLM utilities (`OllamaConnector`) live in the shared **SharedUtils** project and are referenced as a project dependency — no local `utils.cs` copy is needed.

## Example Output

```
Starting sequential translation into 8 languages...
Translated Chinese text
Translated Spanish text
Translated Japanese text
Translated German text
Translated Russian text
Translated Portuguese text
Translated French text
Translated Korean text
Saved translation to translations/README_CHINESE.md
Saved translation to translations/README_SPANISH.md
Saved translation to translations/README_JAPANESE.md
Saved translation to translations/README_GERMAN.md
Saved translation to translations/README_RUSSIAN.md
Saved translation to translations/README_PORTUGUESE.md
Saved translation to translations/README_FRENCH.md
Saved translation to translations/README_KOREAN.md

Total sequential translation time: 42.1234 seconds

=== Translation Complete ===
Translations saved to: translations
============================
```

