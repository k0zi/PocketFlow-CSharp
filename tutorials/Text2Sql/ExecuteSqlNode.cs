using Microsoft.Data.Sqlite;
using PocketFlow;

namespace Text2Sql;

/// <summary>
/// Executes the generated SQL against the database.
/// On success stores results in <c>shared["final_result"]</c>;
/// on failure increments <c>shared["debug_attempts"]</c> and returns <c>"error_retry"</c>
/// (or sets <c>shared["final_error"]</c> when max retries are exhausted).
/// Port of <c>ExecuteSQL</c> in nodes.py.
/// </summary>
public class ExecuteSqlNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return ((string)store["db_path"], (string)store["generated_sql"]);
    }

    protected override object? Execute(object? prepRes)
    {
        var (dbPath, sqlQuery) = ((string, string))prepRes!;

        try
        {
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sqlQuery;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            bool isSelect = sqlQuery.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
                            || sqlQuery.TrimStart().StartsWith("WITH",   StringComparison.OrdinalIgnoreCase);

            if (isSelect)
            {
                using var reader = cmd.ExecuteReader();
                sw.Stop();
                Console.WriteLine($"SQL executed in {sw.Elapsed.TotalSeconds:F3} seconds.");

                var columns = new List<string>();
                for (int i = 0; i < reader.FieldCount; i++)
                    columns.Add(reader.GetName(i));

                var rows = new List<object?[]>();
                while (reader.Read())
                {
                    var row = new object?[reader.FieldCount];
                    reader.GetValues(row!);
                    rows.Add(row);
                }
                return new SqlExecResult { Success = true, Rows = rows, Columns = columns };
            }
            else
            {
                int affected = cmd.ExecuteNonQuery();
                sw.Stop();
                Console.WriteLine($"SQL executed in {sw.Elapsed.TotalSeconds:F3} seconds.");
                return new SqlExecResult
                {
                    Success = true,
                    NonSelectMessage = $"Query OK. Rows affected: {affected}"
                };
            }
        }
        catch (SqliteException ex)
        {
            Console.WriteLine($"SQLite Error during execution: {ex.Message}");
            return new SqlExecResult { Success = false, Error = ex.Message };
        }
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store  = (Dictionary<string, object>)shared;
        var result = (SqlExecResult)execRes!;

        if (result.Success)
        {
            Console.WriteLine("\n===== SQL EXECUTION SUCCESS =====\n");

            if (result.Rows is not null && result.Columns is not null)
            {
                if (result.Columns.Count > 0)
                {
                    Console.WriteLine(string.Join(" | ", result.Columns));
                    int sepLen = result.Columns.Sum(c => c.Length) + 3 * (result.Columns.Count - 1);
                    Console.WriteLine(new string('-', Math.Max(sepLen, 1)));
                }
                if (result.Rows.Count == 0)
                    Console.WriteLine("(No results found)");
                else
                    foreach (var row in result.Rows)
                        Console.WriteLine(string.Join(" | ", row.Select(v => v?.ToString() ?? "NULL")));

                store["final_result"]   = result.Rows;
                store["result_columns"] = result.Columns;
            }
            else
            {
                Console.WriteLine(result.NonSelectMessage);
                store["final_result"] = result.NonSelectMessage ?? string.Empty;
            }

            Console.WriteLine("\n=================================\n");
            return "default";
        }
        else
        {
            store["execution_error"] = result.Error ?? string.Empty;
            int debugAttempts = (int)store.GetValueOrDefault("debug_attempts", 0) + 1;
            store["debug_attempts"] = debugAttempts;
            int maxAttempts = (int)store.GetValueOrDefault("max_debug_attempts", 3);

            Console.WriteLine($"\n===== SQL EXECUTION FAILED (Attempt {debugAttempts}) =====\n");
            Console.WriteLine($"Error: {result.Error}");
            Console.WriteLine("=========================================\n");

            if (debugAttempts >= maxAttempts)
            {
                Console.WriteLine($"Max debug attempts ({maxAttempts}) reached. Stopping.");
                store["final_error"] =
                    $"Failed to execute SQL after {maxAttempts} attempts. Last error: {result.Error}";
                return "default";
            }

            Console.WriteLine("Attempting to debug the SQL...");
            return "error_retry";
        }
    }
}