using PocketFlow;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Node that saves the processed image to the "output" subdirectory.
/// C# port of SaveImage from nodes.py (pocketflow-batch-flow cookbook).
/// </summary>
class SaveImageNode : Node
{
    protected override object? Prepare(object shared)
    {
        Directory.CreateDirectory("output");

        var store = (Dictionary<string, object>)shared;
        var filteredImage = (Image<Rgba32>)store["filtered_image"];

        var inputName = Path.GetFileNameWithoutExtension(Params["input"].ToString()!);
        var filterName = Params["filter"].ToString()!;
        var outputPath = Path.Combine("output", $"{inputName}_{filterName}.jpg");

        return (filteredImage, outputPath);
    }

    protected override object? Execute(object? prepRes)
    {
        var (image, outputPath) = ((Image<Rgba32>, string))prepRes!;
        image.Save(outputPath, new JpegEncoder());
        return outputPath;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        Console.WriteLine($"Saved filtered image to: {execRes}");
        return "default";
    }
}

