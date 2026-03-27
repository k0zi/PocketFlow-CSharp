using System.Text.RegularExpressions;
using PocketFlow;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Agent;

public class DecideActionNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var context  = store.TryGetValue("context", out var c) ? (string)c : "No previous search";
        var question = (string)store["question"];
        return (question, context);
    }

    protected override object? Execute(object? prepRes)
    {
        var (question, context) = ((string, string))prepRes!;

        Console.WriteLine("🤔 Agent deciding what to do next...");

        var prompt = $"""
                      ### CONTEXT
                      You are a research assistant that can search the web.
                      Question: {question}
                      Previous Research: {context}

                      ### ACTION SPACE
                      [1] search
                        Description: Look up more information on the web
                        Parameters:
                          - query (str): What to search for

                      [2] answer
                        Description: Answer the question with current knowledge
                        Parameters:
                          - answer (str): Final answer to the question

                      ## NEXT ACTION
                      Decide the next action based on the context and available actions.
                      Return your response in this format:

                      ```yaml
                      thinking: |
                          <your step-by-step reasoning process>
                      action: search OR answer
                      reason: |
                          <why you chose this action - always use block scalar>
                      answer: |
                          <if action is answer - always use block scalar, leave empty if searching>
                      search_query: <specific search query if action is search (plain string)>
                      ```
                      IMPORTANT: Make sure to:
                      1. ALWAYS use the | block scalar for thinking, reason and answer so colons or quotes inside the text do not break YAML.
                      2. Use proper indentation (4 spaces) for all multi-line fields under |.
                      3. Keep search_query as a single line string without the | character.
                      """;

        var response = OllamaConnector.CallLlm(prompt);
        return ParseYamlResponse(response);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store    = (Dictionary<string, object>)shared;
        var decision = (Dictionary<object, object>)execRes!;
        var action   = decision["action"]?.ToString() ?? "answer";

        if (action == "search")
        {
            store["search_query"] = decision["search_query"]?.ToString() ?? string.Empty;
            Console.WriteLine($"🔍 Agent decided to search for: {store["search_query"]}");
        }
        else
        {
            // Save context in case the LLM answers without searching
            store["context"] = decision["answer"]?.ToString() ?? string.Empty;
            Console.WriteLine("💡 Agent decided to answer the question");
        }

        return action;
    }

    // ── YAML helpers (mirrors the Python two-pass fallback) ──────────────────

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

    private static readonly IDeserializer _yamlDeserializer =
        new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

    private static Dictionary<object, object> ParseYamlSafely(string block)
    {
        // First pass — try parsing as-is
        try
        {
            return _yamlDeserializer.Deserialize<Dictionary<object, object>>(block)
                   ?? throw new InvalidOperationException("YAML deserialized to null.");
        }
        catch (YamlException)
        {
            // Second pass — rewrite bare scalar lines to block scalars
            var fixedLines = new List<string>();
            var blockScalarKeys = new HashSet<string>
                { "thinking", "reason", "answer", "search_query" };

            foreach (var line in block.Split('\n'))
            {
                var keyMatch = Regex.Match(line, @"^(\w+):\s*(.*)$");
                if (keyMatch.Success
                    && blockScalarKeys.Contains(keyMatch.Groups[1].Value)
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
                return _yamlDeserializer.Deserialize<Dictionary<object, object>>(fixedBlock)
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