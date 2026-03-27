using System.Text.RegularExpressions;
using PocketFlow;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace ThoughtActionObservation;

/// <summary>
/// ThinkNode – decides the next action to take given the query and all
/// observations gathered so far.  Returns "action" to proceed or "end" when
/// a final answer has been formulated.
/// Ported from nodes.py :: ThinkNode.
/// </summary>
public class ThinkNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;

        var query = store.TryGetValue("query", out var q) ? (string)q : "";
        var observations = store.TryGetValue("observations", out var obs)
            ? (List<string>)obs
            : new List<string>();

        var currentNumber = store.TryGetValue("current_thought_number", out var ctn)
            ? Convert.ToInt32(ctn)
            : 0;

        // Increment thought counter
        store["current_thought_number"] = currentNumber + 1;

        var observationsText = observations.Count > 0
            ? string.Join("\n", observations.Select((o, i) => $"Observation {i + 1}: {o}"))
            : "No observations yet.";

        return (query, observationsText, currentNumber + 1);
    }

    protected override object? Execute(object? prepRes)
    {
        var (query, observationsText, currentNumber) = ((string, string, int))prepRes!;

        var prompt = $"""
            You are an AI assistant solving a problem. Based on the user's query and previous
            observations, think about what action to take next.

            User query: {query}

            Previous observations:
            {observationsText}

            Return your thinking and decision in this YAML format:
            ```yaml
            thinking: |
                <detailed thinking process>
            action: search OR answer
            action_input: |
                <input for the action, e.g. search query or final answer text>
            is_final: false
            ```
            Set is_final to true ONLY when providing the final answer.
            IMPORTANT: Use | block scalar for thinking and action_input.
            """;

        var response = OllamaConnector.CallLlm(prompt);
        var thoughtData = ParseYamlResponse(response);
        thoughtData["thought_number"] = currentNumber;
        return thoughtData;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var data  = (Dictionary<object, object>)execRes!;

        // Accumulate thoughts
        if (!store.ContainsKey("thoughts"))
            store["thoughts"] = new List<Dictionary<object, object>>();
        ((List<Dictionary<object, object>>)store["thoughts"]).Add(data);

        var action      = data.TryGetValue("action", out var a)       ? a?.ToString() ?? "answer" : "answer";
        var actionInput = data.TryGetValue("action_input", out var ai) ? ai?.ToString() ?? ""      : "";

        store["current_action"]       = action;
        store["current_action_input"] = actionInput;

        // is_final can be bool or string depending on the LLM
        var isFinal = data.TryGetValue("is_final", out var f) && f switch
        {
            bool b   => b,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase),
            _        => false
        };

        if (isFinal)
        {
            store["final_answer"] = actionInput;
            Console.WriteLine($"🎯 Final Answer: {actionInput}");
            return "end";
        }

        var thoughtNumber = data.TryGetValue("thought_number", out var tn) ? tn : "?";
        Console.WriteLine($"🤔 Thought {thoughtNumber}: Decided to execute '{action}'");
        return "action";
    }

    // ── YAML helpers ─────────────────────────────────────────────────────────

    private static readonly IDeserializer YamlDeserializer =
        new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

    private static Dictionary<object, object> ParseYamlResponse(string llmResponse)
    {
        var block = ExtractYamlBlock(llmResponse);
        return ParseYamlSafely(block);
    }

    private static string ExtractYamlBlock(string text)
    {
        var match = Regex.Match(text, @"```yaml(.*?)```",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }

    private static readonly HashSet<string> BlockScalarKeys =
        new() { "thinking", "action_input", "reason" };

    private static Dictionary<object, object> ParseYamlSafely(string block)
    {
        try
        {
            return YamlDeserializer.Deserialize<Dictionary<object, object>>(block)
                   ?? throw new InvalidOperationException("YAML deserialized to null.");
        }
        catch (YamlException)
        {
            // Second pass – rewrite bare scalar lines to block scalars
            var fixedLines = new List<string>();
            foreach (var line in block.Split('\n'))
            {
                var keyMatch = Regex.Match(line, @"^(\w+):\s*(.*)$");
                if (keyMatch.Success
                    && BlockScalarKeys.Contains(keyMatch.Groups[1].Value)
                    && !line.Contains('|'))
                {
                    var key = keyMatch.Groups[1].Value;
                    var val = keyMatch.Groups[2].Value.Trim();
                    fixedLines.Add($"{key}: |");
                    if (!string.IsNullOrEmpty(val))
                        fixedLines.Add($"  {val}");
                }
                else
                {
                    fixedLines.Add(line);
                }
            }

            var fixedBlock = string.Join("\n", fixedLines);
            try
            {
                return YamlDeserializer.Deserialize<Dictionary<object, object>>(fixedBlock)
                       ?? throw new InvalidOperationException("YAML deserialized to null.");
            }
            catch (YamlException ex)
            {
                throw new InvalidOperationException(
                    $"Unable to parse LLM YAML response:\n{block}", ex);
            }
        }
    }
}

