using Microsoft.Data.Sqlite;
using PocketFlow;

namespace Text2Sql;

/// <summary>
/// Connects to the SQLite database and extracts the full schema (table + column names).
/// Stores the result in <c>shared["schema"]</c>.
/// Port of <c>GetSchema</c> in nodes.py.
/// </summary>
public class GetSchemaNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (string)store["db_path"];
    }

    protected override object? Execute(object? prepRes)
    {
        var dbPath = (string)prepRes!;
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        var schema = new List<string>();

        using var tablesCmd = conn.CreateCommand();
        tablesCmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";

        var tables = new List<string>();
        using (var r = tablesCmd.ExecuteReader())
            while (r.Read())
                tables.Add(r.GetString(0));

        foreach (var table in tables)
        {
            schema.Add($"Table: {table}");

            using var infoCmd = conn.CreateCommand();
            infoCmd.CommandText = $"PRAGMA table_info({table})";
            using var ir = infoCmd.ExecuteReader();
            while (ir.Read())
                schema.Add($"  - {ir.GetString(1)} ({ir.GetString(2)})");

            schema.Add("");
        }

        return string.Join("\n", schema).Trim();
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["schema"] = (string)execRes!;

        Console.WriteLine("\n===== DB SCHEMA =====\n");
        Console.WriteLine(execRes);
        Console.WriteLine("\n=====================\n");
        return "default";
    }
}