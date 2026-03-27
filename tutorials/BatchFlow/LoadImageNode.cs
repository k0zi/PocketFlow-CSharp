using PocketFlow;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Node that loads an image file from the "images" subdirectory.
/// C# port of LoadImage from nodes.py (pocketflow-batch-flow cookbook).
/// </summary>
class LoadImageNode : Node
{
    protected override object? Prepare(object shared)
    {
        var input = Params["input"].ToString()!;
        return Path.Combine("images", input);
    }

    protected override object? Execute(object? prepRes)
    {
        var imagePath = (string)prepRes!;
        return Image.Load<Rgba32>(imagePath);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["image"] = execRes!;
        return "apply_filter";
    }
}

