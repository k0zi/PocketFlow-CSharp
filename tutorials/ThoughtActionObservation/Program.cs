using PocketFlow;
using ThoughtActionObservation;

// ── Build the TAO flow (mirrors flow.py) ─────────────────────────────────────
//
//   ThinkNode ──"action"──► ActionNode ──"observe"──► ObserveNode
//       ▲                                                   │
//       └───────────────────"think"────────────────────────┘
//       │
//       └──"end"──► EndNode

var think   = new ThinkNode();
var action  = new ActionNode();
var observe = new ObserveNode();
var end     = new EndNode();

think.On("action").Then(action);   // think → execute action
think.On("end").Then(end);         // think → terminal when final answer is ready
action.On("observe").Then(observe); // action → observe result
observe.On("think").Then(think);   // observe → loop back to think

var flow = new Flow(start: think);

// ── Query (mirrors main.py) ───────────────────────────────────────────────────

const string defaultQuery = "I need to understand the latest developments in artificial intelligence";

var query = args.Length > 0 ? string.Join(" ", args) : defaultQuery;

// ── Run ───────────────────────────────────────────────────────────────────────

var shared = new Dictionary<string, object>
{
    ["query"]                 = query,
    ["thoughts"]              = new List<Dictionary<object, object>>(),
    ["observations"]          = new List<string>(),
    ["current_thought_number"] = 0
};

Console.WriteLine($"🔎 Query: {query}\n");
flow.Run(shared);

Console.WriteLine("\n── Final Answer ─────────────────────────────────────────────────────────────");
Console.WriteLine(shared.TryGetValue("final_answer", out var answer)
    ? answer
    : "Flow did not produce a final answer.");

