using PocketFlow;

/// <summary>
/// Applies the selected text transformation to the input string.
/// C# port of TextTransform from pocketflow-flow/flow.py.
/// </summary>
class TextTransformNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return ((string)store["text"], (string)store["choice"]);
    }

    protected override object? Execute(object? prepRes)
    {
        var (text, choice) = ((string, string))prepRes!;

        return choice switch
        {
            "1" => text.ToUpper(),
            "2" => text.ToLower(),
            "3" => new string(text.Reverse().ToArray()),
            "4" => string.Join(" ", text.Split(' ', StringSplitOptions.RemoveEmptyEntries)),
            _   => "Invalid option!"
        };
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        Console.WriteLine($"\nResult: {execRes}");

        Console.Write("\nConvert another text? (y/n): ");
        var answer = Console.ReadLine() ?? "n";

        if (answer.Trim().Equals("y", StringComparison.OrdinalIgnoreCase))
        {
            var store = (Dictionary<string, object>)shared;
            store.Remove("text");
            return "input";
        }

        return "exit";
    }
}

