using PocketFlow;

namespace Agent;

public class SearchWebNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (string)store["search_query"];
    }

    protected override object? Execute(object? prepRes)
    {
        var query = (string)prepRes!;
        Console.WriteLine($"🌐 Searching the web for: {query}");
        return WebSearchUtils.SearchWebDuckDuckGo(query);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store    = (Dictionary<string, object>)shared;
        var query    = (string)prepRes!;
        var results  = (string)execRes!;
        var previous = store.TryGetValue("context", out var c) ? (string)c : string.Empty;

        store["context"] = previous + $"\n\nSEARCH: {query}\nRESULTS: {results}";

        Console.WriteLine("📚 Found information, analyzing results...");
        return "decide";
    }
}