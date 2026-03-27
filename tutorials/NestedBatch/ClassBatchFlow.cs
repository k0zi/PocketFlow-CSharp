using PocketFlow;

/// <summary>
/// BatchFlow that iterates over every student file inside a class folder.
/// Merges the <c>student</c> parameter into each inner-flow run alongside the
/// inherited <c>class</c> parameter set by the outer <see cref="SchoolBatchFlow"/>.
/// C# port of ClassBatchFlow from flow.py (pocketflow-nested-batch cookbook).
/// </summary>
class ClassBatchFlow : BatchFlow
{
    public ClassBatchFlow(BaseNode start) : base(start) { }

    protected override object? Prepare(object shared)
    {
        var classFolder = Params["class"].ToString()!;
        var classPath   = Path.Combine("school", classFolder);

        return Directory.GetFiles(classPath, "*.txt")
            .Select(Path.GetFileName)
            .OrderBy(f => f)
            .Select(f => new Dictionary<string, object> { ["student"] = f! })
            .ToList();
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store     = (Dictionary<string, object>)shared;
        var className = Params["class"].ToString()!;
        var results   = (Dictionary<string, Dictionary<string, double>>)store["results"];
        var avg       = results[className].Values.Average();
        var label     = className.Contains('_')
            ? className.Split('_')[1].ToUpper()
            : className.ToUpper();

        Console.WriteLine($"Class {label} Average: {avg:F2}\n");
        return "default";
    }
}

