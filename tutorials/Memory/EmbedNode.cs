using OllamaSharp.Models.Chat;
using PocketFlow;

namespace Memory;

/// <summary>
/// Removes the oldest user/assistant pair from <c>shared["messages"]</c>,
/// embeds it, and stores the <see cref="VectorItem"/> in <c>shared["vector_items"]</c>.
/// Always returns <c>"question"</c> to continue the chat loop.
/// Port of <c>EmbedNode</c> in <c>nodes.py</c>.
/// </summary>
public class EmbedNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store    = (Dictionary<string, object>)shared;
        var messages = (List<Message>)store["messages"];
        if (messages.Count <= 6) return null;

        // Extract and remove the oldest pair
        var oldestPair = messages.GetRange(0, 2);
        store["messages"] = messages.GetRange(2, messages.Count - 2);

        return oldestPair;
    }

    protected override object? Execute(object? prepRes)
    {
        if (prepRes is null) return null;

        var conversation = (List<Message>)prepRes;
        var userContent  = conversation.FirstOrDefault(m => m.Role == ChatRole.User)?.Content
                           ?? string.Empty;
        var asstContent  = conversation.FirstOrDefault(m => m.Role == ChatRole.Assistant)?.Content
                           ?? string.Empty;

        var combined  = $"User: {userContent} Assistant: {asstContent}";
        var embedding = OllamaConnector.GetEmbedding(combined);

        return (conversation, embedding);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        if (execRes is null)
            return "question";

        var store = (Dictionary<string, object>)shared;
        if (!store.ContainsKey("vector_items"))
            store["vector_items"] = new List<VectorItem>();

        var items                        = (List<VectorItem>)store["vector_items"];
        var (conversation, embedding)    = ((List<Message>, float[]))execRes!;
        items.Add(new VectorItem(conversation, embedding));

        Console.WriteLine($"✅ Added conversation to index at position {items.Count - 1}");
        Console.WriteLine($"✅ Index now contains {items.Count} conversations");

        return "question";
    }
}