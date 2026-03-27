using OllamaSharp.Models.Chat;
using PocketFlow;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

/// <summary>
/// Validates that user input is travel-related before passing it to the LLM.
/// Uses basic heuristics first, then an LLM-based YAML evaluation.
/// </summary>
class GuardrailNode : Node
{
    private static readonly IDeserializer Deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.GetValueOrDefault("user_input") as string ?? string.Empty;
    }

    protected override object? Execute(object? prepRes)
    {
        var userInput = (string)prepRes!;

        if (string.IsNullOrWhiteSpace(userInput))
            return (false, "Your query is empty. Please provide a travel-related question.");

        if (userInput.Trim().Length < 3)
            return (false, "Your query is too short. Please provide more details about your travel question.");

        var prompt = $"""
Evaluate if the following user query is related to travel advice, destinations, planning, or other travel topics.
The chat should ONLY answer travel-related questions and reject any off-topic, harmful, or inappropriate queries.
User query: {userInput}
Return your evaluation in YAML format:
```yaml
valid: true/false
reason: [Explain why the query is valid or invalid]
```
""";

        var messages = new List<Message>
        {
            new() { Role = ChatRole.User, Content = prompt }
        };

        var response = OllamaConnector.CallLlm(messages);

        // Extract and parse YAML block
        var yamlBlock = ExtractYamlBlock(response);
        var result = Deserializer.Deserialize<Dictionary<object, object>>(yamlBlock);

        var isValid = result.TryGetValue("valid", out var v) &&
                      v?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;

        var reason = result.TryGetValue("reason", out var r)
            ? r?.ToString() ?? "No reason provided."
            : "No reason provided.";

        return (isValid, reason);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var (isValid, message) = ((bool, string))execRes!;

        if (!isValid)
        {
            Console.WriteLine($"\nTravel Advisor: {message}");
            return "retry"; // Loop back to user input
        }

        // Append validated user message to conversation history
        var store = (Dictionary<string, object>)shared;
        var messages = (List<Message>)store["messages"];
        messages.Add(new Message { Role = ChatRole.User, Content = (string)store["user_input"] });

        return "process";
    }

    private static string ExtractYamlBlock(string text)
    {
        var match = Regex.Match(text, @"```yaml(.*?)```",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }
}

