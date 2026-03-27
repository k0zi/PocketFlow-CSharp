using PocketFlow;

/// <summary>
/// Node that calculates a student's average grade and stores it in the shared results map.
/// C# port of CalculateAverage from nodes.py (pocketflow-nested-batch cookbook).
/// </summary>
class CalculateAverageNode : Node
{
    protected override object? Prepare(object shared)
        => ((Dictionary<string, object>)shared)["grades"];

    protected override object? Execute(object? prepRes)
        => ((List<double>)prepRes!).Average();

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store     = (Dictionary<string, object>)shared;
        var className = Params["class"].ToString()!;
        var student   = Params["student"].ToString()!;
        var average   = (double)execRes!;

        if (!store.ContainsKey("results"))
            store["results"] = new Dictionary<string, Dictionary<string, double>>();

        var results = (Dictionary<string, Dictionary<string, double>>)store["results"];
        if (!results.ContainsKey(className))
            results[className] = new Dictionary<string, double>();

        results[className][student] = average;

        Console.WriteLine($"  - {student}: Average = {average:F1}");
        return "default";
    }
}

