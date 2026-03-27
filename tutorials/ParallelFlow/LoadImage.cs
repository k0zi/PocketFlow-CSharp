using PocketFlow;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ParallelFlow;

/// <summary>
/// Loads an image from disk into the shared store.
/// Mirrors <c>LoadImage</c> in nodes.py.
/// </summary>
public class LoadImage : AsyncNode
{
    // Each parallel task stores its image under a unique key so concurrent
    // executions never overwrite each other's data in the shared dictionary.
    private string TaskKey()
    {
        var imagePath = (string)Params["image_path"];
        var filter    = (string)Params["filter"];
        return $"{Path.GetFileNameWithoutExtension(imagePath)}_{filter}";
    }

    protected override Task<object?> PrepAsync(object shared)
    {
        var imagePath = (string)Params["image_path"];
        Console.WriteLine($"Loading image: {imagePath}");
        return Task.FromResult<object?>(imagePath);
    }

    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        var imagePath = (string)prepRes!;
        await Task.Delay(500); // Simulate I/O delay
        return Image.Load<Rgba32>(imagePath);
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store[$"image_{TaskKey()}"] = execRes!;
        return Task.FromResult<object?>("apply_filter");
    }
}