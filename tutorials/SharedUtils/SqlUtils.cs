/// <summary>
/// SQL parsing helpers for LLM-generated YAML responses.
/// Ported from Text2Sql/Utils.cs.
/// </summary>
public static class SqlUtils
{
    /// <summary>
    /// Extracts the SQL string from an LLM response that contains a YAML code block
    /// in the form:
    /// <code>
    /// ```yaml
    /// sql: |
    ///   SELECT ...
    /// ```
    /// </code>
    /// </summary>
    public static string ParseSqlFromYaml(string llmResponse)
    {
        int start = llmResponse.IndexOf("```yaml", StringComparison.OrdinalIgnoreCase);
        if (start < 0)
            throw new InvalidOperationException("No YAML block found in LLM response.");

        start += 7; // skip "```yaml"
        int end = llmResponse.IndexOf("```", start);
        if (end < 0)
            throw new InvalidOperationException("YAML block not properly closed.");

        var yamlStr = llmResponse[start..end].Trim();
        var lines   = yamlStr.Split('\n');

        bool collecting = false;
        var  sqlLines   = new List<string>();

        foreach (var rawLine in lines)
        {
            if (!collecting)
            {
                var trimmed = rawLine.TrimStart();
                if (trimmed.StartsWith("sql:", StringComparison.Ordinal))
                {
                    var afterColon = trimmed[4..].Trim();
                    if (afterColon is "" or "|" or "|-" or "|+" or ">" or ">-" or ">+")
                        collecting = true;
                    else
                        return afterColon.TrimEnd(';').Trim();
                }
            }
            else
            {
                if (rawLine.Trim().Length == 0)
                    sqlLines.Add("");
                else if (rawLine.Length > 0 && (rawLine[0] == ' ' || rawLine[0] == '\t'))
                    sqlLines.Add(rawLine.TrimStart());
                else
                    break;
            }
        }

        return string.Join("\n", sqlLines).Trim().TrimEnd(';');
    }
}

