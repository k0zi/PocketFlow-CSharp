namespace VoiceChat;

internal static class SharedExtensions
{
    public static List<(string Role, string Content)> GetOrAddHistory(this Dictionary<string, object> store)
    {
        if (!store.TryGetValue(Keys.ChatHistory, out var raw) || raw is not List<(string, string)> history)
        {
            history = new List<(string, string)>();
            store[Keys.ChatHistory] = history;
        }
        return history;
    }
}