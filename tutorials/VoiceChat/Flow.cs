using PocketFlow;

namespace VoiceChat;

public static class VoiceChatFlow
{
    /// <summary>
    /// Builds and returns the voice-chat AsyncFlow, mirroring create_voice_chat_flow() in Python.
    /// </summary>
    public static AsyncFlow CreateVoiceChatFlow()
    {
        // Create nodes
        var captureAudio   = new CaptureAudioNode();
        var speechToText   = new SpeechToTextNode();
        var queryLlm       = new QueryLlmNode();
        var textToSpeech   = new TextToSpeechNode();

        // Define transitions
        captureAudio.On("default").Then(speechToText);
        speechToText.On("default").Then(queryLlm);
        queryLlm.On("default").Then(textToSpeech);

        // Loop back for the next turn; "end_conversation" terminates naturally
        textToSpeech.On("next_turn").Then(captureAudio);

        return new AsyncFlow(start: captureAudio);
    }
}

