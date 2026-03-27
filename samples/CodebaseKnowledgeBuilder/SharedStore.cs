namespace CodebaseKnowledgeBuilder;

internal static class SharedStore
{
    public static T Get<T>(Dictionary<string, object> store, string key, T defaultValue)
        => store.TryGetValue(key, out var v) && v is T t ? t : defaultValue;
}