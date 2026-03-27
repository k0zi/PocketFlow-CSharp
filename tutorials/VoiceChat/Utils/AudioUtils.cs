using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;
using PortAudioSharp;

namespace VoiceChat.Utils;

public static class AudioUtils
{
    public const int    DefaultSampleRate           = 44100;
    public const int    DefaultChannels             = 1;
    public const int    DefaultChunkSizeMs          = 50;
    public const float  DefaultSilenceThresholdRms  = 0.01f;
    public const int    DefaultMinSilenceDurationMs = 1000;
    public const int    DefaultMaxRecordingDurationS = 15;
    public const int    DefaultPreRollChunks         = 3;

    /// <summary>
    /// Records audio from the default input device using RMS-based Voice Activity Detection.
    /// Returns (audioData, sampleRate) or (null, sampleRate) if nothing was captured.
    /// </summary>
    public static (float[]? audioData, int sampleRate) RecordAudio(
        int   sampleRate           = DefaultSampleRate,
        int   channels             = DefaultChannels,
        int   chunkSizeMs          = DefaultChunkSizeMs,
        float silenceThresholdRms  = DefaultSilenceThresholdRms,
        int   minSilenceDurationMs = DefaultMinSilenceDurationMs,
        int   maxRecordingDurationS = DefaultMaxRecordingDurationS,
        int   preRollChunksCount   = DefaultPreRollChunks)
    {
        int chunkSizeFrames = sampleRate * chunkSizeMs / 1000;
        int minSilenceChunks = minSilenceDurationMs / chunkSizeMs;
        int maxChunks = maxRecordingDurationS * 1000 / chunkSizeMs;

        Console.WriteLine($"Listening... (max {maxRecordingDurationS}s). Speak when ready.");
        Console.WriteLine($"(Silence threshold RMS: {silenceThresholdRms}, Min silence duration: {minSilenceDurationMs}ms)");

        var audioQueue     = new BlockingCollection<float[]>(200);
        var recordedFrames = new List<float[]>();
        var preRollFrames  = new List<float[]>();
        bool isRecording      = false;
        int  silenceCounter   = 0;
        bool noSpeechDetected = false;

        PortAudio.Initialize();
        try
        {
            var inputParams = new StreamParameters
            {
                device          = PortAudio.DefaultInputDevice,
                channelCount    = channels,
                sampleFormat    = SampleFormat.Float32,
                suggestedLatency = PortAudio.GetDeviceInfo(PortAudio.DefaultInputDevice).defaultLowInputLatency
            };

            using var stream = new PortAudioSharp.Stream(
                inParams:        inputParams,
                outParams:       null,
                sampleRate:      sampleRate,
                framesPerBuffer: (uint)chunkSizeFrames,
                streamFlags:     StreamFlags.ClipOff,
                callback: (nint input, nint output, uint frameCount,
                           ref StreamCallbackTimeInfo timeInfo,
                           StreamCallbackFlags statusFlags, nint userData) =>
                {
                    if (input != nint.Zero)
                    {
                        var chunk = new float[frameCount * (uint)channels];
                        Marshal.Copy(input, chunk, 0, chunk.Length);
                        audioQueue.TryAdd(chunk);
                    }
                    return StreamCallbackResult.Continue;
                },
                userData: nint.Zero);

            stream.Start();

            for (int i = 0; i < maxChunks; i++)
            {
                if (!audioQueue.TryTake(out var audioChunk, chunkSizeMs * 2))
                    continue;

                float rms = CalculateRms(audioChunk);

                if (isRecording)
                {
                    recordedFrames.Add(audioChunk);
                    if (rms < silenceThresholdRms)
                    {
                        silenceCounter++;
                        if (silenceCounter >= minSilenceChunks)
                        {
                            Console.WriteLine("Silence detected, stopping recording.");
                            break;
                        }
                    }
                    else
                    {
                        silenceCounter = 0;
                    }
                }
                else
                {
                    preRollFrames.Add(audioChunk);
                    if (preRollFrames.Count > preRollChunksCount)
                        preRollFrames.RemoveAt(0);

                    if (rms > silenceThresholdRms)
                    {
                        Console.WriteLine("Speech detected, starting recording.");
                        isRecording = true;
                        recordedFrames.AddRange(preRollFrames);
                        preRollFrames.Clear();
                    }
                }

                if (i == maxChunks - 1 && !isRecording)
                {
                    Console.WriteLine("No speech detected within the maximum recording duration.");
                    noSpeechDetected = true;
                    break;
                }
            }

            stream.Stop();
        }
        finally
        {
            PortAudio.Terminate();
        }

        if (noSpeechDetected || recordedFrames.Count == 0)
        {
            if (recordedFrames.Count == 0 && !noSpeechDetected)
                Console.WriteLine("No audio was recorded.");
            return (null, sampleRate);
        }

        int totalSamples = recordedFrames.Sum(f => f.Length);
        var audioData = new float[totalSamples];
        int offset = 0;
        foreach (var frame in recordedFrames)
        {
            Array.Copy(frame, 0, audioData, offset, frame.Length);
            offset += frame.Length;
        }

        Console.WriteLine($"Recording finished. Total duration: {(double)audioData.Length / sampleRate:F2}s");
        return (audioData, sampleRate);
    }

    /// <summary>Plays a float32 PCM array through the default output device.</summary>
    public static void PlayAudioData(float[] audioData, int sampleRate, int channels = 1)
    {
        Console.WriteLine($"Playing audio (Sample rate: {sampleRate} Hz, " +
                          $"Duration: {(double)audioData.Length / (sampleRate * channels):F2}s)");

        PortAudio.Initialize();
        try
        {
            var outputParams = new StreamParameters
            {
                device          = PortAudio.DefaultOutputDevice,
                channelCount    = channels,
                sampleFormat    = SampleFormat.Float32,
                suggestedLatency = PortAudio.GetDeviceInfo(PortAudio.DefaultOutputDevice).defaultLowOutputLatency
            };

            int pos  = 0;
            var done = new ManualResetEventSlim(false);

            using var stream = new PortAudioSharp.Stream(
                inParams:        null,
                outParams:       outputParams,
                sampleRate:      sampleRate,
                framesPerBuffer: 512,
                streamFlags:     StreamFlags.ClipOff,
                callback: (nint input, nint output, uint frameCount,
                           ref StreamCallbackTimeInfo timeInfo,
                           StreamCallbackFlags statusFlags, nint userData) =>
                {
                    int needed    = (int)frameCount * channels;
                    int available = Math.Min(needed, audioData.Length - pos);
                    var buf       = new float[needed];
                    Array.Copy(audioData, pos, buf, 0, available);
                    Marshal.Copy(buf, 0, output, needed);
                    pos += available;
                    if (pos >= audioData.Length)
                    {
                        done.Set();
                        return StreamCallbackResult.Complete;
                    }
                    return StreamCallbackResult.Continue;
                },
                userData: nint.Zero);

            stream.Start();
            done.Wait();
            Thread.Sleep(100); // let the final buffer drain
            stream.Stop();
        }
        finally
        {
            PortAudio.Terminate();
        }

        Console.WriteLine("Playback finished.");
    }

    /// <summary>Calculates the Root Mean Square of a float32 sample array.</summary>
    public static float CalculateRms(float[] samples)
    {
        if (samples.Length == 0) return 0f;
        float sum = 0f;
        foreach (var s in samples) sum += s * s;
        return (float)Math.Sqrt(sum / samples.Length);
    }

    /// <summary>Encodes a float32 PCM array as a 16-bit WAV byte array.</summary>
    public static byte[] ToWavBytes(float[] audioData, int sampleRate, int channels = 1)
    {
        short[] pcm = new short[audioData.Length];
        for (int i = 0; i < audioData.Length; i++)
            pcm[i] = (short)(Math.Clamp(audioData[i], -1f, 1f) * 32767f);

        int   dataSize   = pcm.Length * 2;
        int   byteRate   = sampleRate * channels * 2;
        short blockAlign = (short)(channels * 2);

        using var ms     = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        writer.Write(Encoding.ASCII.GetBytes("RIFF"));
        writer.Write(36 + dataSize);
        writer.Write(Encoding.ASCII.GetBytes("WAVE"));
        writer.Write(Encoding.ASCII.GetBytes("fmt "));
        writer.Write(16);             // SubchunkSize (PCM)
        writer.Write((short)1);       // AudioFormat  (PCM = 1)
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write((short)16);      // BitsPerSample
        writer.Write(Encoding.ASCII.GetBytes("data"));
        writer.Write(dataSize);
        foreach (var sample in pcm) writer.Write(sample);

        return ms.ToArray();
    }

    /// <summary>
    /// Decodes a WAV byte array (16-bit or 32-bit float PCM) into a float32 sample array.
    /// </summary>
    public static (float[] samples, int sampleRate, int channels) DecodeWavBytes(byte[] wavBytes)
    {
        using var ms     = new MemoryStream(wavBytes);
        using var reader = new BinaryReader(ms);

        reader.ReadBytes(4); // "RIFF"
        reader.ReadInt32();  // file size
        reader.ReadBytes(4); // "WAVE"

        // fmt chunk
        reader.ReadBytes(4); // "fmt "
        int fmtSize      = reader.ReadInt32();
        reader.ReadInt16(); // AudioFormat (1 = PCM, 3 = IEEE float)
        int channels     = reader.ReadInt16();
        int sampleRate   = reader.ReadInt32();
        reader.ReadInt32(); // ByteRate
        reader.ReadInt16(); // BlockAlign
        int bitsPerSample = reader.ReadInt16();
        if (fmtSize > 16) reader.BaseStream.Seek(fmtSize - 16, SeekOrigin.Current);

        // Scan for data chunk
        while (ms.Position < ms.Length - 8)
        {
            var chunkId   = Encoding.ASCII.GetString(reader.ReadBytes(4));
            int chunkSize = reader.ReadInt32();

            if (chunkId == "data")
            {
                int numSamples = chunkSize / (bitsPerSample / 8);
                var samples    = new float[numSamples];

                if (bitsPerSample == 16)
                    for (int i = 0; i < numSamples; i++)
                        samples[i] = reader.ReadInt16() / 32768f;
                else if (bitsPerSample == 32)
                    for (int i = 0; i < numSamples; i++)
                        samples[i] = reader.ReadSingle();

                return (samples, sampleRate, channels);
            }

            reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
        }

        throw new InvalidDataException("WAV 'data' chunk not found.");
    }
}





