using PocketFlow;

/// <summary>
/// Outer BatchFlow that iterates over every class folder inside the school directory.
/// Produces a <c>class</c> parameter for each iteration that is consumed by the
/// inner <see cref="ClassBatchFlow"/>.
/// C# port of SchoolBatchFlow from flow.py (pocketflow-nested-batch cookbook).
/// </summary>
class SchoolBatchFlow : BatchFlow
{
    public SchoolBatchFlow(BaseNode start) : base(start) { }

    protected override object? Prepare(object shared)
        => Directory.GetDirectories("school")
            .Select(d => Path.GetFileName(d)!)
            .OrderBy(d => d)
            .Select(c => new Dictionary<string, object> { ["class"] = c })
            .ToList();

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store   = (Dictionary<string, object>)shared;
        var results = (Dictionary<string, Dictionary<string, double>>)store["results"];
        var avg     = results.Values.SelectMany(r => r.Values).Average();

        Console.WriteLine($"School Average: {avg:F2}");
        return "default";
    }
}

