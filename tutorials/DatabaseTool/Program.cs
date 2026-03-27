using PocketFlow;
using DatabaseTool;

// ── Configuration ─────────────────────────────────────────────────────────────

const string DbFile          = "example.db";
const string ExampleTitle    = "Example Task";
const string ExampleDesc     = "This is an example task created using PocketFlow";

// ── Shared state (mirrors the Python dict) ────────────────────────────────────

var shared = new Dictionary<string, object>
{
    ["db_path"]          = DbFile,
    ["task_title"]       = ExampleTitle,
    ["task_description"] = ExampleDesc,
};

// ── Build the flow (mirrors flow.py) ─────────────────────────────────────────

var initDb     = new InitDatabaseNode();
var createTask = new CreateTaskNode();
var listTasks  = new ListTasksNode();

initDb.Then(createTask).Then(listTasks);

var flow = new Flow(start: initDb);

// ── Run ───────────────────────────────────────────────────────────────────────

flow.Run(shared);

// ── Print results (mirrors main.py) ──────────────────────────────────────────

Console.WriteLine($"Database Status: {shared.GetValueOrDefault("db_status")}");
Console.WriteLine($"Task Status: {shared.GetValueOrDefault("task_status")}");
Console.WriteLine("\nAll Tasks:");

if (shared.TryGetValue("tasks", out var tasksObj) && tasksObj is List<object?[]> tasks)
{
    foreach (var task in tasks)
    {
        Console.WriteLine($"- ID: {task[0]}");
        Console.WriteLine($"  Title: {task[1]}");
        Console.WriteLine($"  Description: {task[2]}");
        Console.WriteLine($"  Status: {task[3]}");
        Console.WriteLine($"  Created: {task[4]}");
        Console.WriteLine();
    }
}
