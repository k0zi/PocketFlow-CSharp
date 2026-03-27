using PocketFlow;

namespace ParallelFlow;

// ── ImageBatchFlow ────────────────────────────────────────────────────────────

/// <summary>
/// Processes every image × filter combination <b>sequentially</b>.
/// Mirrors <c>ImageBatchFlow</c> in flow.py.
/// </summary>
public class ImageBatchFlow : AsyncBatchFlow
{
    public ImageBatchFlow(BaseNode start) : base(start) { }

    /// <summary>
    /// Returns one parameter dictionary per (image, filter) combination.
    /// </summary>
    protected override Task<object?> PrepAsync(object shared)
    {
        var store   = (Dictionary<string, object>)shared;
        var images  = (List<string>)store["images"];
        var filters = new[] { "grayscale", "blur", "sepia" };

        var @params = new List<Dictionary<string, object>>();
        foreach (var imagePath in images)
        foreach (var filter in filters)
            @params.Add(new Dictionary<string, object>
            {
                ["image_path"] = imagePath,
                ["filter"]     = filter
            });

        Console.WriteLine($"Processing {images.Count} images with {filters.Length} filters...");
        Console.WriteLine($"Total combinations: {@params.Count}");
        return Task.FromResult<object?>(@params);
    }
}

// ── ImageParallelBatchFlow ────────────────────────────────────────────────────

/// <summary>
/// Processes every image × filter combination <b>in parallel</b>.
/// Mirrors <c>ImageParallelBatchFlow</c> in flow.py.
/// </summary>
public class ImageParallelBatchFlow : AsyncParallelBatchFlow
{
    public ImageParallelBatchFlow(BaseNode start) : base(start) { }

    /// <summary>
    /// Returns one parameter dictionary per (image, filter) combination.
    /// </summary>
    protected override Task<object?> PrepAsync(object shared)
    {
        var store   = (Dictionary<string, object>)shared;
        var images  = (List<string>)store["images"];
        var filters = new[] { "grayscale", "blur", "sepia" };

        var @params = new List<Dictionary<string, object>>();
        foreach (var imagePath in images)
        foreach (var filter in filters)
            @params.Add(new Dictionary<string, object>
            {
                ["image_path"] = imagePath,
                ["filter"]     = filter
            });

        Console.WriteLine($"Processing {images.Count} images with {filters.Length} filters...");
        Console.WriteLine($"Total combinations: {@params.Count}");
        return Task.FromResult<object?>(@params);
    }
}

// ── FlowFactory ───────────────────────────────────────────────────────────────

/// <summary>
/// Builds the node pipeline and wraps it in both batch flow types.
/// Mirrors <c>create_flows()</c> in flow.py.
/// </summary>
public static class FlowFactory
{
    public static (ImageBatchFlow BatchFlow, ImageParallelBatchFlow ParallelBatchFlow) CreateFlows()
    {
        // Build the linear pipeline: Load → ApplyFilter → Save
        var load        = new LoadImage();
        var applyFilter = new ApplyFilter();
        var save        = new SaveImage();

        load.On("apply_filter").Then(applyFilter);
        applyFilter.On("save").Then(save);

        // Both flows share the same pipeline definition; the flow clones nodes
        // internally before each run so there is no shared mutable state.
        return (new ImageBatchFlow(load), new ImageParallelBatchFlow(load));
    }
}

