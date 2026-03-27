using OpenAI.Chat;

/// <summary>
/// OpenAI utility helpers shared across projects.
/// Ported from utils/call_llm.py and tools/vision.py
/// (pocketflow-tool-pdf-vision cookbook).
/// </summary>
public static class OpenAiUtils
{
    private const string DefaultVisionModel = "gpt-4o";

    // ── Vision / multimodal ──────────────────────────────────────────────────

    /// <summary>
    /// Sends a PNG image together with a text prompt to the OpenAI Vision API
    /// (default model: <c>gpt-4o</c>) and returns the model's reply.
    /// Reads <c>OPENAI_API_KEY</c> from the environment.
    /// </summary>
    /// <param name="pngBytes">Raw PNG bytes of the image to analyse.</param>
    /// <param name="prompt">
    /// Instruction for the model.  Defaults to a general OCR prompt when
    /// <see langword="null"/>.
    /// </param>
    /// <param name="model">Chat model to use.</param>
    public static async Task<string> ExtractTextFromImageAsync(
        byte[] pngBytes,
        string? prompt = null,
        string model = DefaultVisionModel)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                     ?? throw new InvalidOperationException(
                         "OPENAI_API_KEY environment variable is not set.");

        prompt ??= "Please extract all text from this image.";

        var client = new ChatClient(model, apiKey);

        var message = new UserChatMessage(
            ChatMessageContentPart.CreateTextPart(prompt),
            ChatMessageContentPart.CreateImagePart(
                BinaryData.FromBytes(pngBytes), "image/png"));

        var result = await client.CompleteChatAsync(
            new ChatMessage[] { message });

        return result.Value.Content[0].Text;
    }

    /// <summary>
    /// Synchronous wrapper around <see cref="ExtractTextFromImageAsync"/>.
    /// </summary>
    public static string ExtractTextFromImage(
        byte[] pngBytes,
        string? prompt = null,
        string model = DefaultVisionModel)
        => ExtractTextFromImageAsync(pngBytes, prompt, model)
           .GetAwaiter().GetResult();
}

