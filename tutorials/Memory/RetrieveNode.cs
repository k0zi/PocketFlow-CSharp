using OllamaSharp.Models.Chat;
using PocketFlow;

namespace Memory;

/// <summary>
/// Finds the most relevant archived conversation using vector similarity (L2).
/// Stores the result in <c>shared["retrieved_conversation"]</c> and returns <c>"answer"</c>.
/// Port of <c>RetrieveNode</c> in <c>nodes.py</c>.
/// </summary>
public class RetrieveNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store    = (Dictionary<string, object>)shared;
        var messages = (List<Message>)store["messages"];

        // Get the latest user message to use as query
        var latestUserMsg = messages.LastOrDefault(m => m.Role == ChatRole.User);
        if (latestUserMsg is null) return null;

        // Nothing to retrieve yet if the archive is empty
        if (!store.TryGetValue("vector_items", out var vi) ||
            vi is not List<VectorItem> items || items.Count == 0)
            return null;

        return (latestUserMsg.Content ?? string.Empty, items);
    }

    protected override object? Execute(object? prepRes)
    {
        if (prepRes is null) return null;

        var (query, items) = ((string, List<VectorItem>))prepRes!;
        var preview        = query.Length > 30 ? query[..30] : query;
        Console.WriteLine($"🔍 Finding relevant conversation for: {preview}...");

        var queryEmbedding        = OllamaConnector.GetEmbedding(query);
        var (bestIdx, bestDist)   = SearchVectors(items, queryEmbedding);

        return (items[bestIdx].Conversation, bestDist);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;

        if (execRes is not null)
        {
            var (conversation, distance) = ((List<Message>, float))execRes!;
            store["retrieved_conversation"] = conversation;
            Console.WriteLine($"📄 Retrieved conversation (distance: {distance:F4})");
        }
        else
        {
            store.Remove("retrieved_conversation");
        }

        return "answer";
    }

    // ── Vector search helpers ─────────────────────────────────────────────────

    private static (int index, float distance) SearchVectors(List<VectorItem> items, float[] query)
    {
        var bestIdx  = 0;
        var bestDist = float.MaxValue;

        for (var i = 0; i < items.Count; i++)
        {
            var dist = L2Squared(query, items[i].Embedding);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestIdx  = i;
            }
        }

        return (bestIdx, bestDist);
    }

    private static float L2Squared(float[] a, float[] b)
    {
        var sum = 0f;
        var len = Math.Min(a.Length, b.Length);
        for (var i = 0; i < len; i++)
        {
            var d = a[i] - b[i];
            sum += d * d;
        }
        return sum;
    }
}