using PocketFlow;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// Node that applies a filter to an image stored in shared state.
/// Delegates filter logic to <see cref="ImageUtils"/>.
/// C# port of ApplyFilter from nodes.py (pocketflow-batch-flow cookbook).
/// </summary>
class ApplyFilterNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var image = (Image<Rgba32>)store["image"];
        var filter = Params["filter"].ToString()!;
        return (image, filter);
    }

    protected override object? Execute(object? prepRes)
    {
        var (image, filterType) = ((Image<Rgba32>, string))prepRes!;
        return ImageUtils.ApplyFilter(image, filterType);
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["filtered_image"] = execRes!;
        return "save";
    }
}

