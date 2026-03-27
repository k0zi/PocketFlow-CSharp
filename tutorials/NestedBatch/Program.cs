using PocketFlow;

// C# port of main.py + flow.py from the pocketflow-nested-batch cookbook.
// Calculates average grades for students, classes, and the whole school
// using nested BatchFlows.

CreateSampleData();

Console.WriteLine("Processing school grades...\n");

// --- Build the base Flow (single student pipeline) ---
var load = new LoadGradesNode();
var calc = new CalculateAverageNode();

load.On("calculate").Then(calc);

var baseFlow = new Flow(start: load);

// --- Wrap in ClassBatchFlow (all students in a class) ---
var classFlow = new ClassBatchFlow(start: baseFlow);

// --- Wrap in SchoolBatchFlow (all classes in the school) ---
var schoolFlow = new SchoolBatchFlow(start: classFlow);

schoolFlow.Run(new Dictionary<string, object>());

// ─── Helper ──────────────────────────────────────────────────────────────────
static void CreateSampleData()
{
    var data = new Dictionary<string, Dictionary<string, double[]>>
    {
        ["class_a"] = new()
        {
            ["student1.txt"] = [7.5, 8.0, 9.0],
            ["student2.txt"] = [8.5, 7.0, 9.5]
        },
        ["class_b"] = new()
        {
            ["student3.txt"] = [6.5, 8.5, 7.0],
            ["student4.txt"] = [9.0, 9.5, 8.0]
        }
    };

    foreach (var (className, students) in data)
    {
        Directory.CreateDirectory(Path.Combine("school", className));
        foreach (var (fileName, grades) in students)
            File.WriteAllLines(
                Path.Combine("school", className, fileName),
                grades.Select(g => g.ToString()));
    }
}
