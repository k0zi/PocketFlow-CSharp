using PocketFlow;

// C# port of main.py from the pocketflow-hello-world cookbook.
// Builds a single-node flow that answers a question using the local LLM.

var answerNode = new AnswerNode();
var qaFlow = new Flow(start: answerNode);

var shared = new Dictionary<string, object>
{
    ["question"] = "In one sentence, what's the end of universe?",
    ["answer"]   = string.Empty
};

qaFlow.Run(shared);

Console.WriteLine($"Question: {shared["question"]}");
Console.WriteLine($"Answer:   {shared["answer"]}");
