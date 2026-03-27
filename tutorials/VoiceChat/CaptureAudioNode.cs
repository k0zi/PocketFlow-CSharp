using PocketFlow;
using VoiceChat.Utils;

namespace VoiceChat;

/// <summary>Records audio from the microphone using VAD, mirroring CaptureAudioNode in Python.</summary>
public class CaptureAudioNode : AsyncNode
{
    protected override Task<object?> PrepAsync(object shared) => Task.FromResult<object?>(null);

    protected override Task<object?> ExecAsync(object? prepRes)
    {
        Console.WriteLine("\nListening for your query...");
        var (audioData, sampleRate) = AudioUtils.RecordAudio();
        return Task.FromResult<object?>((audioData, sampleRate));
    }

    protected override Task<object?> PostAsync(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var (audioData, sampleRate) = ((float[]?, int))execRes!;

        if (audioData is null)
        {
            store[Keys.UserAudioData]       = (object?)null!;
            store[Keys.UserAudioSampleRate] = 0;
            Console.WriteLine("CaptureAudioNode: Failed to capture audio.");
            return Task.FromResult<object?>("end_conversation");
        }

        store[Keys.UserAudioData]       = audioData;
        store[Keys.UserAudioSampleRate] = sampleRate;
        Console.WriteLine($"Audio captured ({(double)audioData.Length / sampleRate:F2}s), proceeding to STT.");
        return Task.FromResult<object?>("default");
    }
}