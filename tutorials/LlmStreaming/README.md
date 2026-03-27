# LLM Streaming and Interruption

Demonstrates real-time LLM response streaming with user-interrupt capability — ported to C# from the Python `pocketflow-llm-streaming` cookbook example.

## Features

- Real-time display of LLM response tokens as they arrive
- Interrupt streaming at any time by pressing **ENTER**
- Fake-streaming mode for local testing (no Ollama required)
- Clean cancellation via `CancellationTokenSource`

## Run It

```bash
cd src
dotnet run --project LlmStreaming
```

## How It Works

### `StreamNode` (AsyncNode)

| Phase | What it does |
|-------|-------------|
| **PrepAsync** | Reads `prompt` from the shared store; creates a `CancellationTokenSource` |
| **ExecAsync** | Starts a background task listening for ENTER; streams tokens from the LLM, printing each one in real-time; handles interruption via `OperationCanceledException` |
| **PostAsync** | Cancels the listener task so it doesn't linger; disposes the `CancellationTokenSource` |

### Streaming helpers (`SharedUtils/OllamaConnector`)

| Method | Purpose |
|--------|---------|
| `FakeStreamLlmAsync` | Yields pre-defined text in 10-character chunks — useful for local demos |
| `StreamLlmAsync` | Streams real tokens from Ollama using `ChatAsync` with `Stream = true` |

## Switching to Real LLM Streaming

In `StreamNode.cs`, change the one line in `ExecAsync`:

```csharp
// From (fake / local demo):
await foreach (var token in OllamaConnector.FakeStreamLlmAsync(prompt, cancellationToken: cts.Token))

// To (real Ollama):
await foreach (var token in OllamaConnector.StreamLlmAsync(prompt, cancellationToken: cts.Token))
```

Make sure Ollama is running locally:

```bash
ollama serve
```

Set the model via environment variable (defaults to `gemma3:latest`):

```bash
export OLLAMA_MODEL="llama3.2:latest"
```

## Files

| File | Description |
|------|-------------|
| `StreamNode.cs` | PocketFlow `AsyncNode` that drives the streaming loop |
| `Program.cs` | Wires up the flow and sets the initial prompt |
| `../SharedUtils/OllamaConnector.cs` | Shared `StreamLlmAsync` / `FakeStreamLlmAsync` helpers |

