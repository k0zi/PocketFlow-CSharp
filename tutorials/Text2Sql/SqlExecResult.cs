namespace Text2Sql;

/// <summary>Carries the outcome of a SQL execution attempt.</summary>
internal sealed class SqlExecResult
{
    public bool Success { get; init; }
    /// <summary>SELECT rows (non-null when <see cref="Success"/> is true and the query is a SELECT).</summary>
    public List<object?[]>? Rows { get; init; }
    /// <summary>Column names that match <see cref="Rows"/>.</summary>
    public List<string>? Columns { get; init; }
    /// <summary>Message for non-SELECT success (e.g. INSERT/UPDATE rowcount).</summary>
    public string? NonSelectMessage { get; init; }
    /// <summary>SQLite error message when <see cref="Success"/> is false.</summary>
    public string? Error { get; init; }
}