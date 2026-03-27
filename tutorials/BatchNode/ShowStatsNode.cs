using PocketFlow;

/// <summary>
/// Node that displays the final aggregated CSV statistics.
/// C# port of <c>ShowStats</c> from the pocketflow-batch-node cookbook (flow.py).
/// </summary>
class ShowStatsNode : Node
{
    /// <summary>Retrieves the statistics dictionary from the shared store.</summary>
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store["statistics"];
    }

    /// <summary>Prints the aggregated statistics to the console.</summary>
    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var stats = (Dictionary<string, object>)prepRes!;

        Console.WriteLine("\nFinal Statistics:");
        Console.WriteLine($"  Total Sales:        ${(double)stats["total_sales"]:N2}");
        Console.WriteLine($"  Average Sale:       ${(double)stats["average_sale"]:N2}");
        Console.WriteLine($"  Total Transactions: {(int)stats["total_transactions"]:N0}");
        Console.WriteLine();

        return null;
    }
}

