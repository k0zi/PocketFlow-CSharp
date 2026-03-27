using PocketFlow;

namespace Rag;

/// <summary>
/// Reads <c>shared["texts"]</c> (chunks), embeds each one, and stores the
/// resulting vectors in <c>shared["embeddings"]</c> as a <c>List&lt;float[]&gt;</c>.
/// Port of <c>EmbedDocumentsNode</c> in <c>nodes.py</c>.
/// </summary>
public class EmbedDocumentsNode : BatchNode
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (IEnumerable<string>)store["texts"];
    }

    protected override object? Execute(object? prepRes)
    {
        var text = (string)prepRes!;
        return OllamaConnector.GetEmbedding(text);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store       = (Dictionary<string, object>)shared;
        var execResList = (List<object?>)execRes!;

        var embeddings = execResList
            .Select(e => (float[])e!)
            .ToList();

        store["embeddings"] = embeddings;
        Console.WriteLine($"✅ Created {embeddings.Count} document embeddings");
        return "default";
    }
}