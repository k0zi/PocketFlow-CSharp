using Docnet.Core;
using Docnet.Core.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

/// <summary>
/// PDF processing utilities shared across projects.
/// Ported from tools/pdf.py (pocketflow-tool-pdf-vision cookbook).
/// </summary>
public static class PdfUtils
{
    /// <summary>
    /// Converts all pages of a PDF file to <see cref="Image{Rgba32}"/> instances.
    /// Each page is rendered so that its largest dimension does not exceed
    /// <paramref name="maxSize"/> pixels (aspect ratio is preserved by Docnet.Core).
    /// </summary>
    /// <param name="pdfPath">Absolute or relative path to the PDF file.</param>
    /// <param name="maxSize">Maximum pixel dimension (width or height) per page.</param>
    /// <returns>
    /// A list of tuples containing the rendered image and its 1-based page number.
    /// </returns>
    public static List<(Image<Rgba32> Image, int PageNumber)> PdfToImages(
        string pdfPath,
        int maxSize = 2000)
    {
        var results = new List<(Image<Rgba32>, int)>();

        // PageDimensions(maxSize, maxSize) tells Docnet.Core the target bounding box;
        // the page is scaled to fit within it while preserving aspect ratio.
        using var docReader = DocLib.Instance.GetDocReader(
            pdfPath,
            new PageDimensions(maxSize, maxSize));

        int pageCount = docReader.GetPageCount();

        for (int i = 0; i < pageCount; i++)
        {
            using var pageReader = docReader.GetPageReader(i);

            // GetImage() returns raw BGRA bytes in Docnet.Core convention.
            byte[] rawBytes = pageReader.GetImage();
            int width  = pageReader.GetPageWidth();
            int height = pageReader.GetPageHeight();

            // Load BGRA pixel data directly, then convert to Rgba32 for interop.
            using var bgraImage = Image.LoadPixelData<Bgra32>(rawBytes, width, height);
            var rgbaImage = bgraImage.CloneAs<Rgba32>();

            results.Add((rgbaImage, i + 1));
        }

        return results;
    }

    /// <summary>
    /// Encodes a <see cref="Image"/> as a PNG and returns the raw bytes.
    /// </summary>
    public static byte[] ImageToPngBytes(Image image)
    {
        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return ms.ToArray();
    }

    /// <summary>
    /// Encodes a <see cref="Image"/> as a PNG and returns a Base64 string.
    /// </summary>
    public static string ImageToBase64(Image image)
        => Convert.ToBase64String(ImageToPngBytes(image));
}

