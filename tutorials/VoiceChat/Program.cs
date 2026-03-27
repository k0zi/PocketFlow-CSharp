using VoiceChat;

Console.WriteLine("Starting PocketFlow Voice Chat...");
Console.WriteLine("Speak your query after 'Listening for your query...' appears.");
Console.WriteLine("Press Ctrl+C to stop the conversation at any time.");

var shared = new Dictionary<string, object>
{
    ["user_audio_data"]        = (object?)null!,
    ["user_audio_sample_rate"] = 0,
    ["chat_history"]           = new List<(string Role, string Content)>(),
    ["continue_conversation"]  = true
};

var flow = VoiceChatFlow.CreateVoiceChatFlow();
await flow.RunAsync(shared);

