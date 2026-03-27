using OllamaSharp.Models.Chat;
using PocketFlow;

/// <summary>
/// Sends validated travel queries to the LLM and appends the reply to the conversation history.
/// </summary>
class LlmNode : Node
{
    private const string SystemPrompt =
        "You are a helpful travel advisor that provides information about destinations, " +
        "travel planning, accommodations, transportation, activities, and other travel-related topics. " +
        "Only respond to travel-related queries and keep responses informative and friendly. " +
        "Your responses are concise in 100 words.";

    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var messages = (List<Message>)store["messages"];

        // Ensure the system prompt is always the first message
        if (!messages.Any(m => m.Role == ChatRole.System))
            messages.Insert(0, new Message { Role = ChatRole.System, Content = SystemPrompt });

        return messages;
    }

    protected override object? Execute(object? prepRes)
    {
        var messages = (List<Message>)prepRes!;
        return OllamaConnector.CallLlm(messages);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var reply = (string)execRes!;
        Console.WriteLine($"\nTravel Advisor: {reply}");

        var store = (Dictionary<string, object>)shared;
        var messages = (List<Message>)store["messages"];
        messages.Add(new Message { Role = ChatRole.Assistant, Content = reply });

        return "continue";
    }
}

