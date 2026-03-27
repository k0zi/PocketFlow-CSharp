using PocketFlow;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ParallelFlow;

/// <summary>
/// Applies a named filter (grayscale / blur / sepia) to a loaded image.
/// Mirrors <c>ApplyFilter</c> in nodes.py.
/// </summary>
public class ApplyFilter : AsyncNode
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
        var image      = (Image<Rgba32>)store[$"image_{TaskKey()}"];
        var filterType = (string)Params["filter"];
        Console.WriteLine($"Applying {filterType} filter...");
        return Task.FromResult<object?>((image, filterType));
    }

    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        var (image, filterType) = ((Image<Rgba32>, string))prepRes!;
        await Task.Delay(500); // Simulate processing delay

        // Clone so the source image is not mutated for other concurrent tasks.
        return filterType switch
        {
            "grayscale" => image.Clone(ctx => ctx.Grayscale()),
            "blur"      => image.Clone(ctx => ctx.GaussianBlur(3)),
            "sepia"     => image.Clone(ctx => ctx.Sepia()),
            _           => throw new ArgumentException($"Unknown filter: {filterType}")
        };
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store[$"filtered_image_{TaskKey()}"] = execRes!;
        return Task.FromResult<object?>("save");
    }
}