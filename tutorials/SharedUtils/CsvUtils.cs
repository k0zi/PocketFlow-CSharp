/// <summary>
/// CSV I/O helpers shared across PocketFlow examples.
/// </summary>
public static class CsvUtils
{
    /// <summary>
    /// Reads a CSV file lazily in fixed-size chunks.
    /// </summary>
    /// <param name="filePath">Path to the CSV file.</param>
    /// <param name="chunkSize">Number of data rows per chunk (default 1 000).</param>
    /// <param name="hasHeader">When <c>true</c>, the first line is treated as a header and skipped.</param>
    /// <returns>
    /// A sequence of chunks; each chunk is a <see cref="List{T}"/> of rows,
    /// where each row is a <c>string[]</c> of column values split on comma.
    /// </returns>
    public static IEnumerable<List<string[]>> ReadChunks(
        string filePath,
        int chunkSize = 1_000,
        bool hasHeader = true)
    {
        using var reader = new StreamReader(filePath, System.Text.Encoding.UTF8);

        if (hasHeader)
            reader.ReadLine(); // discard header

        var chunk = new List<string[]>(chunkSize);

        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            chunk.Add(line.Split(','));

            if (chunk.Count >= chunkSize)
            {
                yield return chunk;
                chunk = new List<string[]>(chunkSize);
            }
        }

        if (chunk.Count > 0)
            yield return chunk;
    }

    /// <summary>
    /// Reads all rows of a CSV file into a single list, optionally skipping the header.
    /// </summary>
    public static List<string[]> ReadAll(string filePath, bool hasHeader = true)
        => ReadChunks(filePath, int.MaxValue, hasHeader)
           .FirstOrDefault() ?? [];
}

