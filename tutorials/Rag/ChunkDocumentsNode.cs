using PocketFlow;

namespace Rag;

/// <summary>
/// Reads <c>shared["texts"]</c>, splits each document into fixed-size chunks,
/// then stores the flattened list back into <c>shared["texts"]</c>.
/// Port of <c>ChunkDocumentsNode</c> in <c>nodes.py</c>.
/// </summary>
public class ChunkDocumentsNode : BatchNode
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (IEnumerable<string>)store["texts"];
    }

    protected override object? Execute(object? prepRes)
    {
        var text = (string)prepRes!;
        return OllamaConnector.FixedSizeChunk(text);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store       = (Dictionary<string, object>)shared;
        var origTexts   = (IEnumerable<string>)prepRes!;
        var execResList = (List<object?>)execRes!;

        var allChunks = new List<string>();
        foreach (var item in execResList)
            allChunks.AddRange((List<string>)item!);

        store["texts"] = allChunks;
        Console.WriteLine($"✅ Created {allChunks.Count} chunks from {origTexts.Count()} documents");
        return "default";
    }
}