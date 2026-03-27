using PocketFlow;

namespace Text2Sql;

/// <summary>
/// When SQL execution fails, asks the LLM to generate a corrected query.
/// Updates <c>shared["generated_sql"]</c> with the fixed SQL.
/// Port of <c>DebugSQL</c> in nodes.py.
/// </summary>
public class DebugSqlNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (
            store.GetValueOrDefault("natural_query") as string,
            store.GetValueOrDefault("schema")        as string,
            store.GetValueOrDefault("generated_sql") as string,
            store.GetValueOrDefault("execution_error") as string
        );
    }

    protected override object? Execute(object? prepRes)
    {
        var (naturalQuery, schema, failedSql, errorMessage) =
            ((string?, string?, string?, string?))prepRes!;

        var prompt = $"""
The following SQLite SQL query failed:
```sql
{failedSql}
```
It was generated for: "{naturalQuery}"
Schema:
{schema}
Error: "{errorMessage}"

Provide a corrected SQLite query.

Respond ONLY with a YAML block containing the corrected SQL under the key 'sql':
```yaml
sql: |
  SELECT ... -- corrected query
```
""";

        var llmResponse = OllamaConnector.CallLlm(prompt);
        return SqlUtils.ParseSqlFromYaml(llmResponse);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["generated_sql"] = (string)execRes!;
        store.Remove("execution_error");

        int debugAttempts = (int)store.GetValueOrDefault("debug_attempts", 0);
        Console.WriteLine($"\n===== REVISED SQL (Attempt {debugAttempts + 1}) =====\n");
        Console.WriteLine(execRes);
        Console.WriteLine("\n====================================\n");
        return "default";
    }
}

