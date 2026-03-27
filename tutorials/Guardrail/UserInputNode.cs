using PocketFlow;

/// <summary>
/// Collects user input and routes to validation or exits the conversation.
/// </summary>
class UserInputNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        if (!store.ContainsKey("messages"))
        {
            store["messages"] = new List<OllamaSharp.Models.Chat.Message>();
            Console.WriteLine("Welcome to the Travel Advisor Chat! Type 'exit' to end the conversation.");
        }
        return null;
    }

    protected override object? Execute(object? prepRes)
    {
        Console.Write("\nYou: ");
        return Console.ReadLine() ?? string.Empty;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var userInput = (string)execRes!;

        if (userInput.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("\nGoodbye! Safe travels!");
            return null; // End the conversation
        }

        var store = (Dictionary<string, object>)shared;
        store["user_input"] = userInput;

        return "validate";
    }
}

