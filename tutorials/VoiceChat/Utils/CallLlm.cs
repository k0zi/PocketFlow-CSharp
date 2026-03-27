using OpenAI.Chat;

namespace VoiceChat.Utils;

public static class CallLlm
{
    private const string DefaultModel = "gpt-4o";

    /// <summary>
    /// Sends the conversation history to OpenAI and returns the assistant reply.
    /// Reads OPENAI_API_KEY from the environment.
    /// </summary>
    public static async Task<string> CallLlmAsync(
        IEnumerable<(string Role, string Content)> chatHistory,
        string model = DefaultModel)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                     ?? throw new InvalidOperationException(
                         "OPENAI_API_KEY environment variable is not set.");

        var client = new ChatClient(model, apiKey);

        var messages = chatHistory.Select<(string Role, string Content), ChatMessage>(entry =>
            entry.Role == "user"
                ? new UserChatMessage(entry.Content)
                : new AssistantChatMessage(entry.Content))
            .ToList();

        var result = await client.CompleteChatAsync(messages);
        return result.Value.Content[0].Text;
    }
}

