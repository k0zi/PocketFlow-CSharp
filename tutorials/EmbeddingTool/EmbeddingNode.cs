using PocketFlow;

namespace EmbeddingTool;

/// <summary>
/// Reads text from the shared store, generates an embedding vector via
/// <see cref="OllamaConnector.GetEmbedding"/>, and writes the result back.
/// C# port of <c>nodes.py / EmbeddingNode</c> from the pocketflow-tool-embeddings cookbook.
/// </summary>
public class EmbeddingNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("text", out var text) ? (string)text : string.Empty;
    }

    protected override object? Execute(object? prepRes)
    {
        var text = (string)prepRes!;
        return OllamaConnector.GetEmbedding(text);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["embedding"] = (float[])execRes!;
        return "default";
    }
}

