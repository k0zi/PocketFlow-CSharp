using PocketFlow;

/// <summary>
/// Node that loads grades from a student's text file.
/// Each line in the file contains a single numeric grade.
/// C# port of LoadGrades from nodes.py (pocketflow-nested-batch cookbook).
/// </summary>
class LoadGradesNode : Node
{
    protected override object? Prepare(object shared)
    {
        var className   = Params["class"].ToString()!;
        var studentFile = Params["student"].ToString()!;
        return Path.Combine("school", className, studentFile);
    }

    protected override object? Execute(object? prepRes)
    {
        var filePath = (string)prepRes!;
        return File.ReadAllLines(filePath)
            .Select(line => double.Parse(line.Trim()))
            .ToList();
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["grades"] = execRes!;
        return "calculate";
    }
}

