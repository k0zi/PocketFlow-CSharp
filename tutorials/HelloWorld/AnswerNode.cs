using PocketFlow;

/// <summary>
/// Reads a question from shared store, calls the LLM, and writes the answer back.
/// C# port of flow.py / AnswerNode from the pocketflow-hello-world cookbook.
/// </summary>
class AnswerNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (string)store["question"];
    }

    protected override object? Execute(object? prepRes)
    {
        var question = (string)prepRes!;
        return OllamaConnector.CallLlm(question);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["answer"] = execRes ?? string.Empty;
        return null; // no transition needed — single-node flow
    }
}

