using PocketFlow;

namespace EmbeddingTool;

/// <summary>
/// Factory that assembles the embedding flow.
/// C# port of <c>flow.py / create_embedding_flow</c> from the pocketflow-tool-embeddings cookbook.
/// </summary>
public static class EmbeddingFlow
{
    /// <summary>Creates a single-node flow that generates an embedding for the supplied text.</summary>
    public static Flow Create()
    {
        var embeddingNode = new EmbeddingNode();
        return new Flow(start: embeddingNode);
    }
}

