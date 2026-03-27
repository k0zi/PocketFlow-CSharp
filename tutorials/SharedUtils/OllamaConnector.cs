using System.Runtime.CompilerServices;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

public sealed class OllamaConnector
{
    private const string OLLAMA_MODEL = "gemma3:latest";
    private const string OLLAMA_HOST  = "http://localhost:11434";
    private const string OLLAMA_EMBED_MODEL = "embeddinggemma"; //"nomic-embed-text";
    // ── LLM ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends a prompt as a single user turn and returns the model reply.
    /// The model and host are read from <c>OLLAMA_MODEL</c> and <c>OLLAMA_HOST</c>.
    /// </summary>
    public static string CallLlm(string prompt)
    {
        var messages = new List<Message>
        {
            new() { Role = ChatRole.User, Content = prompt }
        };
        return CallLlm(messages);
    }

    /// <summary>
    /// Sends a full message history to the model and returns the reply.
    /// </summary>
    public static string CallLlm(List<Message> messages)
    {
        var host = Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? OLLAMA_HOST;
        var model = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? OLLAMA_MODEL;
        var client = new OllamaApiClient(host);

        var request = new ChatRequest { Model = model, Messages = messages, Stream = false };

        ChatResponseStream? result = null;
        Task.Run(async () =>
        {
            await foreach (var chunk in client.ChatAsync(request))
                result = chunk;
        }).GetAwaiter().GetResult();

        return result?.Message?.Content ?? string.Empty;
    }

    // ── Embeddings ───────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the embedding vector for <paramref name="text"/> using the model
    /// specified by <c>OLLAMA_EMBED_MODEL</c> (default: <c>nomic-embed-text</c>).
    /// The Ollama host is read from <c>OLLAMA_HOST</c> (default: <c>http://localhost:11434</c>).
    /// </summary>
    public static float[] GetEmbedding(string text)
    {
        var host  = Environment.GetEnvironmentVariable("OLLAMA_HOST")        ?? OLLAMA_HOST;
        var model = Environment.GetEnvironmentVariable("OLLAMA_EMBED_MODEL") ?? OLLAMA_EMBED_MODEL;

        var client = new OllamaApiClient(host);

        float[]? embedding = null;
        Task.Run(async () =>
        {
            var req = new EmbedRequest { Model = model, Input = new List<string> { text } };
            var res = await client.EmbedAsync(req);
            embedding = res.Embeddings is { Count: > 0 } e ? e[0] : null;
        }).GetAwaiter().GetResult();

        return embedding ?? Array.Empty<float>();
    }

    // ── Async LLM ────────────────────────────────────────────────────────────

    /// <summary>
    /// Sends <paramref name="prompt"/> to the LLM asynchronously on a thread-pool
    /// thread so async flows are never blocked.
    /// </summary>
    public static Task<string> CallLlmAsync(string prompt) =>
        Task.Run(() => CallLlm(prompt));

    // ── Chunking ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Splits <paramref name="text"/> into fixed-size chunks of at most
    /// <paramref name="chunkSize"/> characters.
    /// </summary>
    public static List<string> FixedSizeChunk(string text, int chunkSize = 2000)
    {
        var chunks = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
            chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
        return chunks;
    }

    // ── Streaming ────────────────────────────────────────────────────────────

    /// <summary>
    /// Streams LLM response tokens from Ollama one chunk at a time.
    /// Each yielded value is a partial content string as it arrives.
    /// </summary>
    public static async IAsyncEnumerable<string> StreamLlmAsync(
        string prompt,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var host   = Environment.GetEnvironmentVariable("OLLAMA_HOST")  ?? OLLAMA_HOST;
        var model  = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? OLLAMA_MODEL;
        var client = new OllamaApiClient(host);

        var messages = new List<Message> { new() { Role = ChatRole.User, Content = prompt } };
        var request  = new ChatRequest { Model = model, Messages = messages, Stream = true };

        await foreach (var chunk in client.ChatAsync(request).WithCancellation(cancellationToken))
        {
            var content = chunk?.Message?.Content;
            if (!string.IsNullOrEmpty(content))
                yield return content;
        }
    }

    private const string FakeStreamText =
        "This is a fake response. Today is a sunny day. The sun is shining. " +
        "The birds are singing. The flowers are blooming. The bees are buzzing. " +
        "The wind is blowing. The clouds are drifting. The sky is blue. " +
        "The grass is green. The trees are tall. The water is clear. The fish are swimming.";

    /// <summary>
    /// Returns pre-defined text in fixed-size chunks without a real LLM call.
    /// Useful for testing streaming logic locally.
    /// </summary>
    public static async IAsyncEnumerable<string> FakeStreamLlmAsync(
        string prompt,
        string? predefinedText = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var text      = predefinedText ?? FakeStreamText;
        const int chunkSize = 10;
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
            await Task.Yield(); // allow cancellation checks between chunks
        }
    }
}