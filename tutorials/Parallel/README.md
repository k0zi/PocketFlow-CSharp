# Parallel Batch Translation Process

This project demonstrates using PocketFlow's async and parallel features (`AsyncFlow`, `AsyncParallelBatchNode`) to translate a document into multiple languages concurrently.

## Goal

Translate `README.md` into multiple languages (Chinese, Spanish, Japanese, German, Russian, Portuguese, French, Korean) in parallel, saving each to a file in the `translations/` directory. The main goal is to compare execution time against a sequential process.

## Getting Started

1. Make sure [Ollama](https://ollama.com/) is running locally:
   ```bash
   ollama serve
   ```

2. Pull the required model (default: `gemma3:latest`):
   ```bash
   ollama pull gemma3:latest
   ```

3. Optionally override the Ollama host or model via environment variables:
   ```bash
   export OLLAMA_HOST="http://localhost:11434"
   export OLLAMA_MODEL="gemma3:latest"
   ```

4. Run the translation process from the `src/` directory:
   ```bash
   dotnet run --project Parallel
   ```

## How It Works

The implementation uses an `AsyncParallelBatchNode` (`TranslateTextNodeParallel`) that processes all translation requests concurrently:

1. **`PrepAsync`** – Reads the source text and target languages from the shared store. Returns one `(text, language)` pair per language as the batch input.

2. **`ExecAsync`** – Receives one `(text, language)` pair, builds a translation prompt, and calls the LLM via `Utils.CallLlmAsync`. All languages are processed in parallel thanks to `AsyncParallelBatchNode`.

3. **`PostAsync`** – Collects all results and writes each translated file (`translations/README_LANGUAGE.md`) asynchronously with `File.WriteAllTextAsync`.

This approach leverages `Task.WhenAll` under the hood to fire off all LLM calls simultaneously, significantly reducing total wall-clock time compared to a sequential run.

## Example Output & Comparison

```
# --- Sequential Run ---
Starting sequential translation into 8 languages...
Translated Chinese text
...
Translated Korean text
Saved translation to translations/README_CHINESE.md
...
Total sequential translation time: ~1136 seconds

=== Translation Complete ===
Translations saved to: translations
============================


# --- Parallel Run (this project) ---
Starting parallel translation into 8 languages...
Translated French text
Translated Portuguese text
Translated Spanish text
...
Saved translation to translations/README_CHINESE.md
...
Total parallel translation time: ~209 seconds

=== Translation Complete ===
Translations saved to: translations
============================
```

*(Actual times will vary based on LLM response speed and hardware.)*

## Files

| File | Description |
|------|-------------|
| `Program.cs` | Entry point – loads `README.md`, builds the `AsyncFlow`, runs it and reports timing |
| `Nodes.cs` | `TranslateTextNodeParallel` – the `AsyncParallelBatchNode` that drives all translations |
| `Utils.cs` | `CallLlmAsync` – async wrapper around `OllamaConnector.CallLlm` |
| `Parallel.csproj` | Project file with references to `PocketFlow` and `SharedUtils` |
| `translations/` | Output directory (created automatically at runtime) |

