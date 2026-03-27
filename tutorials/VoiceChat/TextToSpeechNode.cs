using PocketFlow;
using VoiceChat.Utils;

namespace VoiceChat;

/// <summary>Converts the last LLM reply to audio and plays it back, mirroring TextToSpeechNode in Python.</summary>
public class TextToSpeechNode : AsyncNode
{
    protected override Task<object?> PrepAsync(object shared)
    {
        var store   = (Dictionary<string, object>)shared;
        var history = SharedExtensions.GetOrAddHistory(store);

        if (history.Count == 0)
        {
            Console.WriteLine("TextToSpeechNode: Chat history is empty.");
            return Task.FromResult<object?>(null);
        }

        var last = history[^1];
        if (last.Role == "assistant" && !string.IsNullOrEmpty(last.Content))
            return Task.FromResult<object?>(last.Content);

        Console.WriteLine("TextToSpeechNode: Last message is not from assistant. Skipping TTS.");
        return Task.FromResult<object?>(null);
    }

    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        if (prepRes is null) return null;

        Console.WriteLine("Converting LLM response to speech...");
        var wavBytes = await TextToSpeech.TextToSpeechApiAsync((string)prepRes);
        return wavBytes;
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (execRes is null)
        {
            Console.WriteLine("TextToSpeechNode: TTS failed or was skipped.");
            return Task.FromResult<object?>("next_turn");
        }

        try
        {
            var wavBytes = (byte[])execRes;
            var (samples, sampleRate, channels) = AudioUtils.DecodeWavBytes(wavBytes);
            Console.WriteLine("Playing LLM response...");
            AudioUtils.PlayAudioData(samples, sampleRate, channels);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing TTS audio: {ex.Message}");
        }

        var store    = (Dictionary<string, object>)shared;
        var cont     = store.ContainsKey(Keys.ContinueConversation) && (bool)store[Keys.ContinueConversation];

        if (cont)
            return Task.FromResult<object?>("next_turn");

        Console.WriteLine("Conversation ended by user flag.");
        return Task.FromResult<object?>("end_conversation");
    }
}