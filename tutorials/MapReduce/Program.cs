using PocketFlow;

// C# port of main.py + flow.py from the pocketflow-map-reduce cookbook.
// Evaluates resumes in a Map-Reduce pattern:
//   ReadResumesNode  →  EvaluateResumesNode (BatchNode)  →  ReduceResultsNode
// LLM calls are handled by OllamaConnector from the shared SharedUtils project.

var shared = new Dictionary<string, object>();

// Build flow (mirrors flow.py create_resume_processing_flow())
var readResumes     = new ReadResumesNode();
var evaluateResumes = new EvaluateResumesNode(maxRetries: 3);
var reduceResults   = new ReduceResultsNode();

readResumes.Then(evaluateResumes).Then(reduceResults);

var flow = new Flow(start: readResumes);

Console.WriteLine("Starting resume qualification processing...");
flow.Run(shared);

// Detailed per-file results
if (shared.ContainsKey("summary") && shared.ContainsKey("evaluations"))
{
    Console.WriteLine("\nDetailed evaluation results:");
    var evaluations = (Dictionary<string, ResumeEvaluation>)shared["evaluations"];
    foreach (var (filename, evaluation) in evaluations)
    {
        var check = evaluation.Qualifies ? "✓" : "✗";
        Console.WriteLine($"{check} {evaluation.CandidateName} ({filename})");
    }
}

Console.WriteLine("\nResume processing complete!");

