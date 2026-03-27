using PocketFlow;
using VoiceChat.Utils;

namespace VoiceChat;

/// <summary>Converts recorded audio to text via OpenAI STT, mirroring SpeechToTextNode in Python.</summary>
public class SpeechToTextNode : AsyncNode
{
    protected override Task<object?> PrepAsync(object shared)
    {
        var store     = (Dictionary<string, object>)shared;
        var audioData = store.GetValueOrDefault(Keys.UserAudioData) as float[];
        var sampleRate = store.ContainsKey(Keys.UserAudioSampleRate)
            ? (int)store[Keys.UserAudioSampleRate] : 0;

        if (audioData is null || sampleRate == 0)
        {
            Console.WriteLine("SpeechToTextNode: No audio data to process.");
            return Task.FromResult<object?>(null);
        }

        return Task.FromResult<object?>((audioData, sampleRate));
    }

    protected override async Task<object?> ExecAsync(object? prepRes)
    {
        if (prepRes is null) return null;

        var (audioData, sampleRate) = ((float[], int))prepRes;
        var wavBytes = AudioUtils.ToWavBytes(audioData, sampleRate);

        Console.WriteLine("Converting speech to text...");
        return await SpeechToText.SpeechToTextApiAsync(wavBytes);
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        if (execRes is null or "")
        {
            Console.WriteLine("SpeechToTextNode: STT API returned no text.");
            return Task.FromResult<object?>("end_conversation");
        }

        var store = (Dictionary<string, object>)shared;
        var text  = (string)execRes;
        Console.WriteLine($"User: {text}");

        var history = SharedExtensions.GetOrAddHistory(store);
        history.Add(("user", text));

        store[Keys.UserAudioData]       = (object?)null!;
        store[Keys.UserAudioSampleRate] = 0;
        return Task.FromResult<object?>("default");
    }
}