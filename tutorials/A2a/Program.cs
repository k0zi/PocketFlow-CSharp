using A2a;
using A2aProtocol;

// ── Argument Parsing ──────────────────────────────────────────────────────────

var mode      = "server";
var host      = "0.0.0.0";
var port      = 10002;
var serverUrl = "http://localhost:10002";

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--client":                          mode      = "client";       break;
        case "--server":                          mode      = "server";       break;
        case "--host" when i + 1 < args.Length:  host      = args[++i];      break;
        case "--port" when i + 1 < args.Length:  port      = int.Parse(args[++i]); break;
        case "--url"  when i + 1 < args.Length:  serverUrl = args[++i];      break;
    }
}

// ── Client Mode ───────────────────────────────────────────────────────────────

if (mode == "client")
{
    Console.WriteLine($"🔗 Connecting to A2A server at {serverUrl} …");
    var resolver  = new A2aCardResolver(serverUrl);
    var agentCard = resolver.GetAgentCard();
    var client    = new A2aClient(agentCard);

    Console.WriteLine($"✅ Connected to agent: {agentCard.Name}");
    Console.WriteLine("Type 'quit' to exit.\n");

    var sessionId = Guid.NewGuid().ToString();

    while (true)
    {
        Console.Write("Describe your expense: ");
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrEmpty(input)) continue;
        if (input is "quit" or "exit" or "q") break;

        var taskId = Guid.NewGuid().ToString();
        Console.WriteLine("📋 Submitting task …");

        await client.SendTaskAsync(new TaskSendParams
        {
            Id        = taskId,
            SessionId = sessionId,
            Message   = new A2aMessage
            {
                Role  = "user",
                Parts = [new TextPart { Text = input }],
            },
        });

        // Poll until terminal state
        A2aTask? task = null;
        while (true)
        {
            var resp = await client.GetTaskAsync(new TaskQueryParams { Id = taskId });
            task = resp.Result;

            if (task?.Status.State
                    is TaskState.Completed
                    or TaskState.Failed
                    or TaskState.Canceled)
                break;

            Console.Write(".");
            await Task.Delay(500);
        }

        Console.WriteLine();

        if (task?.Status.State == TaskState.Completed)
        {
            Console.WriteLine("\n✅ Response:");
            if (task.Artifacts is { Count: > 0 })
                foreach (var artifact in task.Artifacts)
                    foreach (var part in artifact.Parts.OfType<TextPart>())
                        Console.WriteLine(part.Text);
        }
        else
        {
            Console.WriteLine($"\n❌ Task ended with state: {task?.Status.State}");
        }

        Console.WriteLine();
    }

    return;
}

// ── Server Mode ───────────────────────────────────────────────────────────────

Console.WriteLine($"🚀 Starting Expense Reimbursement A2A Server on http://{host}:{port}/ …");

var flow        = ExpenseFlow.Create();
var taskManager = new AgentTaskManager(flow);

var agentCardDef = new AgentCard
{
    Name        = "Expense Reimbursement Agent",
    Description = "An AI agent that processes expense reimbursement requests via the A2A protocol.",
    Url         = $"http://{(host == "0.0.0.0" ? "localhost" : host)}:{port}/",
    Version     = "1.0",
    Capabilities = new AgentCapabilities { Streaming = true },
    Skills =
    [
        new AgentSkillInfo
        {
            Id          = "expense_reimbursement",
            Name        = "Expense Reimbursement",
            Description = "Process and evaluate expense reimbursement requests",
            Tags        = ["expenses", "reimbursement", "finance"],
            Examples    =
            [
                "I need reimbursement for a $45 team lunch yesterday",
                "Reimbursement request for $350 flight to SF for client meeting",
            ],
        },
    ],
};

var server = new A2aServer(agentCardDef, taskManager, host, port);
server.Start(args);
