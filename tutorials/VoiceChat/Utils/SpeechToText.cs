using OpenAI.Audio;

namespace VoiceChat.Utils;

public static class SpeechToText
{
    private const string DefaultModel = "gpt-4o-transcribe";

    /// <summary>
    /// Sends WAV audio bytes to the OpenAI transcription API and returns the transcribed text.
    /// Reads OPENAI_API_KEY from the environment.
    /// </summary>
    public static async Task<string?> SpeechToTextApiAsync(byte[] wavBytes, string model = DefaultModel)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                     ?? throw new InvalidOperationException(
                         "OPENAI_API_KEY environment variable is not set.");

        var client     = new AudioClient(model, apiKey);
        using var ms   = new MemoryStream(wavBytes);
        var transcript = await client.TranscribeAudioAsync(ms, "audio.wav");

        return transcript.Value.Text;
    }
}


