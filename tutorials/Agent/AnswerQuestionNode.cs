using PocketFlow;

namespace Agent;

public class AnswerQuestionNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store   = (Dictionary<string, object>)shared;
        var question = (string)store["question"];
        var context  = store.TryGetValue("context", out var c) ? (string)c : string.Empty;
        return (question, context);
    }

    protected override object? Execute(object? prepRes)
    {
        var (question, context) = ((string, string))prepRes!;

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
        return "done";
    }
}

