using PocketFlow;

// C# port of main.py from the pocketflow-majority-vote cookbook.
// Runs a majority-vote reasoning flow: makes multiple independent LLM attempts
// and returns the most common answer.

var problem = string.Empty;
var numTries = 5;

for (int i = 0; i < args.Length; i++)
{
    if (args[i] == "--problem" && i + 1 < args.Length)
        problem = args[++i];
    else if (args[i] == "--tries" && i + 1 < args.Length && int.TryParse(args[i + 1], out var t))
    {
        numTries = t;
        i++;
    }
}

if (string.IsNullOrWhiteSpace(problem))
{
    problem = """
        You work at a shoe factory. In front of you, there are three pairs of shoes
        (six individual shoes) with the following sizes: two size 4s, two size 5s,
        and two size 6s. The factory defines an "acceptable pair" as two shoes that
        differ in size by a maximum of one size (e.g., a size 5 and a size 6 would
        be an acceptable pair). If you close your eyes and randomly pick three pairs
        of shoes without replacement, what is the probability that you end up drawing
        three acceptable pairs?
        """;
}

var shared = new Dictionary<string, object>
{
    ["question"] = problem,
    ["num_tries"] = numTries
};

Console.WriteLine($"Running majority vote with {numTries} attempt(s)...\n");

var majorityNode = new MajorityVoteNode();
var flow = new Flow(start: majorityNode);
flow.Run(shared);

Console.WriteLine("\n=== Final Answer ===");
Console.WriteLine(shared.TryGetValue("majority_answer", out var answer) ? answer : "(none)");
Console.WriteLine("====================");
