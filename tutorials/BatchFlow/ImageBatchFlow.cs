using PocketFlow;

/// <summary>
/// BatchFlow that processes multiple images with different filters.
/// Generates all image × filter combinations and runs the base Flow for each.
/// C# port of ImageBatchFlow from flow.py (pocketflow-batch-flow cookbook).
/// </summary>
class ImageBatchFlow : BatchFlow
{
    public ImageBatchFlow(BaseNode start) : base(start) { }

    protected override object? Prepare(object shared)
    {
        var images = new[] { "cat.jpg", "dog.jpg", "bird.jpg" };
        var filters = new[] { "grayscale", "blur", "sepia" };

        return images
            .SelectMany(img => filters.Select(f => new Dictionary<string, object>
            {
                ["input"]  = img,
                ["filter"] = f
            }))
            .ToList();
    }
}

