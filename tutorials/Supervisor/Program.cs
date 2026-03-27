using PocketFlow;
using Supervisor;

// ── Inner agent flow (mirrors create_agent_inner_flow in flow.py) ─────────────

var decide = new DecideActionNode();
var search = new SearchWebNode();
var answer = new UnreliableAnswerNode();

decide.On("search").Then(search);   // search branch
decide.On("answer").Then(answer);   // answer branch
search.On("decide").Then(decide);   // loop back after each search

var agentFlow = new Flow(start: decide);

// ── Outer supervised flow (mirrors create_agent_flow in flow.py) ─────────────

var supervisor = new SupervisorNode();

agentFlow.Next(supervisor);             // after inner flow ends → supervisor
supervisor.On("retry").Then(agentFlow); // rejected answer → restart inner flow

var outerFlow = new Flow(start: agentFlow);

// ── Read question from CLI (mirrors main.py) ──────────────────────────────────

const string defaultQuestion = "Who won the Nobel Prize in Physics 2024?";

var question = defaultQuestion;
foreach (var arg in args)
{
    if (arg.StartsWith("--"))
    {
        question = arg[2..];
        break;
    }
}

// ── Run ───────────────────────────────────────────────────────────────────────

var shared = new Dictionary<string, object> { ["question"] = question };

Console.WriteLine($"🤔 Processing question: {question}");
outerFlow.Run(shared);

Console.WriteLine("\n🎯 Final Answer:");
Console.WriteLine(shared.TryGetValue("answer", out var ans) && !string.IsNullOrEmpty((string)ans)
    ? ans
    : "No answer found");
