using OllamaSharp.Models.Chat;
using PocketFlow;

namespace Memory;

/// <summary>
/// Handles interactive user input. Initialises <c>shared["messages"]</c> on the
/// first run, then reads a line from the console and appends a user message.
/// Returns <c>"retrieve"</c> to continue the flow, or <c>null</c> on exit.
/// Port of <c>GetUserQuestionNode</c> in <c>nodes.py</c>.
/// </summary>
public class GetUserQuestionNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        if (!store.ContainsKey("messages"))
        {
            store["messages"] = new List<Message>();
            Console.WriteLine("Welcome to the interactive chat! Type 'exit' to end the conversation.");
        }
        return null;
    }

    protected override object? Execute(object? prepRes)
    {
        Console.Write("\nYou: ");
        var userInput = Console.ReadLine() ?? string.Empty;

        if (userInput.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase))
            return null;

        return userInput;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        if (execRes is null)
        {
            Console.WriteLine("\nGoodbye!");
            return null; // End the conversation
        }

        var store    = (Dictionary<string, object>)shared;
        var messages = (List<Message>)store["messages"];
        messages.Add(new Message { Role = ChatRole.User, Content = (string)execRes });

        return "retrieve";
    }
}