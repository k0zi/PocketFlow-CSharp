using PocketFlow;
using VoiceChat.Utils;

namespace VoiceChat;

/// <summary>Sends conversation history to the LLM and stores the reply, mirroring QueryLLMNode in Python.</summary>
public class QueryLlmNode : AsyncNode
{
    protected override Task<object?> PrepAsync(object shared)
    {
        var store   = (Dictionary<string, object>)shared;
        var history = SharedExtensions.GetOrAddHistory(store);

        if (history.Count == 0)
        {
            Console.WriteLine("QueryLlmNode: Chat history is empty. Skipping LLM call.");
            return Task.FromResult<object?>(null);
        }

        return Task.FromResult<object?>(history);
    }

    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        if (prepRes is null) return null;

        var history = (List<(string Role, string Content)>)prepRes;
        Console.WriteLine("Sending query to LLM...");
        return await CallLlm.CallLlmAsync(history);
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (execRes is null or "")
        {
            Console.WriteLine("QueryLlmNode: LLM returned no response.");
            return Task.FromResult<object?>("end_conversation");
        }

        var store = (Dictionary<string, object>)shared;
        var reply = (string)execRes;
        Console.WriteLine($"LLM: {reply}");

        SharedExtensions.GetOrAddHistory(store).Add(("assistant", reply));
        return Task.FromResult<object?>("default");
    }
}