using PocketFlow;

namespace Text2Sql;

/// <summary>
/// Sends the natural-language query + schema to the LLM and parses the SQL reply.
/// Stores the result in <c>shared["generated_sql"]</c>.
/// Port of <c>GenerateSQL</c> in nodes.py.
/// </summary>
public class GenerateSqlNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return ((string)store["natural_query"], (string)store["schema"]);
    }

    protected override object? Execute(object? prepRes)
    {
        var (naturalQuery, schema) = ((string, string))prepRes!;

        var prompt = $"""
                      Given SQLite schema:
                      {schema}

                      Question: "{naturalQuery}"

                      Respond ONLY with a YAML block containing the SQL query under the key 'sql':
                      ```yaml
                      sql: |
                        SELECT ...
                      ```
                      """;

        var llmResponse = OllamaConnector.CallLlm(prompt);
        return SqlUtils.ParseSqlFromYaml(llmResponse);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["generated_sql"] = (string)execRes!;
        store["debug_attempts"] = 0; // reset on fresh SQL generation

        Console.WriteLine("\n===== GENERATED SQL =====\n");
        Console.WriteLine(execRes);
        Console.WriteLine("\n=========================\n");
        return "default";
    }
}