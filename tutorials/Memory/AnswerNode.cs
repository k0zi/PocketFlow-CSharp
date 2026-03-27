using OllamaSharp.Models.Chat;
using PocketFlow;

namespace Memory;

/// <summary>
/// Builds a prompt from the 3 most recent conversation pairs plus the retrieved
/// past conversation, calls the LLM, and appends the response to the history.
/// Returns <c>"embed"</c> when the sliding window overflows, otherwise <c>"question"</c>.
/// Port of <c>AnswerNode</c> in <c>nodes.py</c>.
/// </summary>
public class AnswerNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store    = (Dictionary<string, object>)shared;
        var messages = (List<Message>)store["messages"];
        if (messages.Count == 0) return null;

        // Keep the last 6 messages (= 3 conversation pairs)
        var recentMessages = messages.Count > 6
            ? messages.GetRange(messages.Count - 6, 6)
            : new List<Message>(messages);

        // Prepend the retrieved relevant conversation when available
        var context = new List<Message>();
        if (store.TryGetValue("retrieved_conversation", out var rc) &&
            rc is List<Message> retrieved)
        {
            context.Add(new Message
            {
                Role    = ChatRole.System,
                Content = "The following is a relevant past conversation that may help with the current query:"
            });
            context.AddRange(retrieved);
            context.Add(new Message
            {
                Role    = ChatRole.System,
                Content = "Now continue the current conversation:"
            });
        }

        context.AddRange(recentMessages);
        return context;
    }

    protected override object? Execute(object? prepRes)
    {
        if (prepRes is null) return null;
        return OllamaConnector.CallLlm((List<Message>)prepRes);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        if (prepRes is null || execRes is null) return null;

        var store    = (Dictionary<string, object>)shared;
        var messages = (List<Message>)store["messages"];
        var reply    = (string)execRes;

        Console.WriteLine($"\nA: {reply}");
        messages.Add(new Message { Role = ChatRole.Assistant, Content = reply });

        // If we have more than 3 conversation pairs, archive the oldest one
        return messages.Count > 6 ? "embed" : "question";
    }
}