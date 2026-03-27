using PocketFlow;

/// <summary>
/// Collects text input from the user and presents the transformation menu.
/// C# port of TextInput from pocketflow-flow/flow.py.
/// </summary>
class TextInputNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;

        if (!store.ContainsKey("text"))
        {
            Console.Write("\nEnter text to convert: ");
            store["text"] = Console.ReadLine() ?? string.Empty;
        }

        return store["text"];
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        Console.WriteLine("\nChoose transformation:");
        Console.WriteLine("1. Convert to UPPERCASE");
        Console.WriteLine("2. Convert to lowercase");
        Console.WriteLine("3. Reverse text");
        Console.WriteLine("4. Remove extra spaces");
        Console.WriteLine("5. Exit");

        Console.Write("\nYour choice (1-5): ");
        var choice = Console.ReadLine() ?? "5";

        if (choice == "5")
            return "exit";

        var store = (Dictionary<string, object>)shared;
        store["choice"] = choice;
        return "transform";
    }
}

