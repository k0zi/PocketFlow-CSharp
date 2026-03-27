using PocketFlow;

/// <summary>
/// Reduce phase: aggregates per-resume evaluation results into a qualification summary.
/// C# port of <c>ReduceResultsNode</c> from the pocketflow-map-reduce cookbook (nodes.py).
/// </summary>
class ReduceResultsNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store["evaluations"];
    }

    protected override object? Execute(object? prepRes)
    {
        var evaluations = (Dictionary<string, ResumeEvaluation>)prepRes!;

        var qualifiedNames = evaluations.Values
            .Where(e => e.Qualifies)
            .Select(e => e.CandidateName)
            .ToList();

        int total = evaluations.Count;
        int qualifiedCount = qualifiedNames.Count;
        double percentage = total > 0 ? Math.Round(qualifiedCount / (double)total * 100, 1) : 0;

        return new Dictionary<string, object>
        {
            ["total_candidates"]    = total,
            ["qualified_count"]     = qualifiedCount,
            ["qualified_percentage"] = percentage,
            ["qualified_names"]     = qualifiedNames,
        };
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store   = (Dictionary<string, object>)shared;
        var summary = (Dictionary<string, object>)execRes!;
        store["summary"] = summary;

        Console.WriteLine("\n===== Resume Qualification Summary =====");
        Console.WriteLine($"Total candidates evaluated: {summary["total_candidates"]}");
        Console.WriteLine($"Qualified candidates: {summary["qualified_count"]} ({summary["qualified_percentage"]}%)");

        var qualifiedNames = (List<string>)summary["qualified_names"];
        if (qualifiedNames.Count > 0)
        {
            Console.WriteLine("\nQualified candidates:");
            foreach (var name in qualifiedNames)
                Console.WriteLine($"- {name}");
        }

        return "default";
    }
}

