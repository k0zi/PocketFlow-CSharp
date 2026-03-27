using PocketFlow;

/// <summary>
/// BatchNode that processes a large CSV file in chunks.
/// C# port of <c>CSVProcessor</c> from the pocketflow-batch-node cookbook (nodes.py).
/// </summary>
/// <remarks>
/// The CSV file is expected to have columns: date, amount, product.
/// Column indices: 0 = date, 1 = amount, 2 = product.
/// </remarks>
class CsvProcessorNode : BatchNode
{
    private readonly int _chunkSize;

    public CsvProcessorNode(int chunkSize = 1_000) : base()
    {
        _chunkSize = chunkSize;
    }

    /// <summary>
    /// Splits the CSV into a list of row-chunks using <see cref="CsvUtils.ReadChunks"/>.
    /// Each chunk is a <c>List&lt;string[]&gt;</c> containing at most <c>chunkSize</c> rows.
    /// </summary>
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var inputFile = (string)store["input_file"];

        // Materialize the lazy sequence so the file is fully read before processing begins.
        return CsvUtils.ReadChunks(inputFile, _chunkSize).ToList();
    }

    /// <summary>
    /// Processes a single chunk: computes total sales and transaction count.
    /// </summary>
    /// <param name="prepRes">A <c>List&lt;string[]&gt;</c> representing one CSV chunk.</param>
    protected override object? Execute(object? prepRes)
    {
        var rows = (List<string[]>)prepRes!;
        double totalAmount = 0;
        int count = 0;

        foreach (var row in rows)
        {
            if (row.Length > 1 &&
                double.TryParse(row[1],
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var amount))
            {
                totalAmount += amount;
                count++;
            }
        }

        return new Dictionary<string, object>
        {
            ["total_sales"]       = totalAmount,
            ["num_transactions"]  = count,
            ["total_amount"]      = totalAmount,
        };
    }

    /// <summary>
    /// Aggregates per-chunk results into final statistics stored in <c>shared["statistics"]</c>.
    /// Returns the action <c>"show_stats"</c> to route to <see cref="ShowStatsNode"/>.
    /// </summary>
    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store   = (Dictionary<string, object>)shared;
        var results = (List<object?>)execRes!;

        double totalSales = 0, totalAmount = 0;
        int    totalTransactions = 0;

        foreach (var item in results)
        {
            var res = (Dictionary<string, object>)item!;
            totalSales       += (double)res["total_sales"];
            totalAmount      += (double)res["total_amount"];
            totalTransactions += (int)res["num_transactions"];
        }

        store["statistics"] = new Dictionary<string, object>
        {
            ["total_sales"]       = totalSales,
            ["average_sale"]      = totalTransactions > 0 ? totalAmount / totalTransactions : 0.0,
            ["total_transactions"] = totalTransactions,
        };

        return "show_stats";
    }
}

