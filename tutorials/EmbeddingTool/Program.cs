// C# port of main.py from the pocketflow-tool-embeddings cookbook.
// Builds a single-node flow that generates a text embedding via OllamaConnector.

using EmbeddingTool;

var flow = EmbeddingFlow.Create();

const string text = "What's the meaning of life?";

var shared = new Dictionary<string, object>
{
    ["text"]      = text,
    ["embedding"] = Array.Empty<float>()
};

flow.Run(shared);

var embedding = (float[])shared["embedding"];

Console.WriteLine($"Text:               {text}");
Console.WriteLine($"Embedding dimension:{embedding.Length}");
Console.WriteLine($"First 5 values:     [{string.Join(", ", embedding.Take(5).Select(v => v.ToString("F6")))}]");
