using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

/// <summary>
/// Image processing utilities shared across projects.
/// Ported from nodes.py (pocketflow-batch-flow cookbook).
/// </summary>
public static class ImageUtils
{
    /// <summary>
    /// Applies the named filter to a clone of the given image and returns the result.
    /// Supported filters: "grayscale", "blur", "sepia".
    /// </summary>
    public static Image<Rgba32> ApplyFilter(Image<Rgba32> image, string filterType)
    {
        var copy = image.Clone();
        switch (filterType)
        {
            case "grayscale":
                copy.Mutate(x => x.Grayscale());
                break;
            case "blur":
                copy.Mutate(x => x.GaussianBlur(3f));
                break;
            case "sepia":
                copy.Mutate(x => x.Sepia());
                break;
            default:
                throw new ArgumentException($"Unknown filter: {filterType}");
        }
        return copy;
    }
}

