using PocketFlow;

namespace Supervisor;

/// <summary>
/// Generates a final answer – but has a 50 % chance of returning a nonsensical
/// dummy answer to demonstrate the supervisor's rejection logic.
/// Ported from <c>UnreliableAnswerNode</c> in nodes.py.
/// </summary>
public class UnreliableAnswerNode : Node
{
    private static readonly Random _rng = new();

    protected override object? Prepare(object shared)
    {
        var store    = (Dictionary<string, object>)shared;
        var question = (string)store["question"];
        var context  = store.TryGetValue("context", out var c) ? (string)c : string.Empty;
        return (question, context);
    }

    protected override object? Execute(object? prepRes)
    {
        var (question, context) = ((string, string))prepRes!;

        // 50 % chance to return a dummy answer
        if (_rng.NextDouble() < 0.5)
        {
            Console.WriteLine("🤪 Generating unreliable dummy answer...");
            return "Sorry, I'm on a coffee break right now. All information I provide is completely made up anyway. " +
                   "The answer to your question is 42, or maybe purple unicorns. Who knows? Certainly not me!";
        }

        Console.WriteLine("✍️  Crafting final answer...");

        var prompt = $"""
                      ### CONTEXT
                      Based on the following information, answer the question.
                      Question: {question}
                      Research: {context}

                      ## YOUR ANSWER:
                      Provide a comprehensive answer using the research results.
                      """;

        return OllamaConnector.CallLlm(prompt);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["answer"] = (string)execRes!;

        Console.WriteLine("✅ Answer generated successfully");
        return null; // end of inner flow — outer flow takes over
    }
}

