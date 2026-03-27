using Microsoft.Data.Sqlite;

/// <summary>
/// SQLite database helpers for PocketFlow nodes.
/// Ported from pocketflow-tool-database/tools/database.py.
/// </summary>
public static class DatabaseUtils
{
    /// <summary>
    /// Executes a SQL query against the specified database file and returns the results.
    /// </summary>
    /// <param name="dbPath">Path to the SQLite database file.</param>
    /// <param name="query">SQL query to execute.</param>
    /// <param name="parameters">
    /// Optional named parameters to prevent SQL injection,
    /// e.g. <c>("$title", "My Task")</c>.
    /// </param>
    /// <returns>
    /// A list of rows (each row is an array of column values) for SELECT queries;
    /// an empty list for non-SELECT queries.
    /// </returns>
    public static List<object?[]> ExecuteSql(
        string dbPath,
        string query,
        IReadOnlyList<(string Name, object? Value)>? parameters = null)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = query;

        if (parameters is not null)
            foreach (var (name, value) in parameters)
                cmd.Parameters.AddWithValue(name, value ?? DBNull.Value);

        var trimmed  = query.TrimStart();
        bool isSelect = trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase)
                     || trimmed.StartsWith("WITH",   StringComparison.OrdinalIgnoreCase);

        if (isSelect)
        {
            using var reader = cmd.ExecuteReader();
            var results = new List<object?[]>();
            while (reader.Read())
            {
                var row = new object?[reader.FieldCount];
                reader.GetValues(row!);
                results.Add(row);
            }
            return results;
        }

        cmd.ExecuteNonQuery();
        return [];
    }

    /// <summary>
    /// Creates the example <c>tasks</c> table if it does not already exist.
    /// </summary>
    public static void InitDb(string dbPath)
    {
        const string createTableSql = """
            CREATE TABLE IF NOT EXISTS tasks (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                title       TEXT NOT NULL,
                description TEXT,
                status      TEXT DEFAULT 'pending',
                created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP
            )
            """;

        ExecuteSql(dbPath, createTableSql);
    }
}

