using PocketFlow;

/// <summary>
/// Map phase: reads all .txt resume files from the <c>data/</c> directory into shared storage.
/// C# port of <c>ReadResumesNode</c> from the pocketflow-map-reduce cookbook (nodes.py).
/// </summary>
class ReadResumesNode : Node
{
    protected override object? Execute(object? prepRes)
    {
        var dataDir = Path.Combine(AppContext.BaseDirectory, "data");
        if (!Directory.Exists(dataDir))
            dataDir = "data"; // fallback to relative path when running with dotnet run

        var resumeFiles = new Dictionary<string, string>();

        foreach (var filePath in Directory.EnumerateFiles(dataDir, "*.txt"))
        {
            var filename = Path.GetFileName(filePath);
            resumeFiles[filename] = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
        }

        Console.WriteLine($"Loaded {resumeFiles.Count} resume(s) from {dataDir}");
        return resumeFiles;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["resumes"] = (Dictionary<string, string>)execRes!;
        return "default";
    }
}

