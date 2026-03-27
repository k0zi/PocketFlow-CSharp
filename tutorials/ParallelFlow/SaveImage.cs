using PocketFlow;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ParallelFlow;

/// <summary>
/// Saves the processed image to the <c>output/</c> directory.
/// Mirrors <c>SaveImage</c> in nodes.py.
/// </summary>
public class SaveImage : AsyncNode
{
    private string TaskKey()
    {
        var imagePath = (string)Params["image_path"];
        var filter    = (string)Params["filter"];
        return $"{Path.GetFileNameWithoutExtension(imagePath)}_{filter}";
    }

    protected override Task<object?> PrepAsync(object shared)
    {
        var store      = (Dictionary<string, object>)shared;
        var image      = (Image<Rgba32>)store[$"filtered_image_{TaskKey()}"];
        var baseName   = Path.GetFileNameWithoutExtension((string)Params["image_path"]);
        var filterType = (string)Params["filter"];
        var outputPath = Path.Combine("output", $"{baseName}_{filterType}.jpg");
        Directory.CreateDirectory("output");
        return Task.FromResult<object?>((image, outputPath));
    }

    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        var (image, outputPath) = ((Image<Rgba32>, string))prepRes!;
        await Task.Delay(500); // Simulate I/O delay
        await image.SaveAsJpegAsync(outputPath);
        return outputPath;
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        Console.WriteLine($"Saved: {execRes}");
        return Task.FromResult<object?>("default");
    }
}


