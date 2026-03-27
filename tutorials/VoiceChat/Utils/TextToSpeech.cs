using OpenAI.Audio;

namespace VoiceChat.Utils;

public static class TextToSpeech
{
    private const string DefaultModel = "gpt-4o-mini-tts";

    /// <summary>
    /// Converts text to WAV audio bytes using the OpenAI TTS API.
    /// Returns the raw WAV bytes ready for decoding via AudioUtils.DecodeWavBytes.
    /// Reads OPENAI_API_KEY from the environment.
    /// </summary>
    public static Task<byte[]> TextToSpeechApiAsync(string text) =>
        TextToSpeechApiAsync(text, DefaultModel, GeneratedSpeechVoice.Alloy);

    public static async Task<byte[]> TextToSpeechApiAsync(
        string text,
        string model,
        GeneratedSpeechVoice voice)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                     ?? throw new InvalidOperationException(
                         "OPENAI_API_KEY environment variable is not set.");

        var client = new AudioClient(model, apiKey);

        var options = new SpeechGenerationOptions
        {
            ResponseFormat = GeneratedSpeechFormat.Wav
        };

        var result = await client.GenerateSpeechAsync(text, voice, options);
        return result.Value.ToArray();
    }
}


