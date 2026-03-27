using PocketFlow;

Console.WriteLine("=== Resume Parser - Structured Output with Indexes & Comments ===\n");

// --- Configuration ---
var targetSkills = new List<string>
{
    "Team leadership & management", // 0
    "CRM software",                 // 1
    "Project management",           // 2
    "Public speaking",              // 3
    "Microsoft Office",             // 4
    "Python",                       // 5
    "Data Analysis"                 // 6
};

const string resumeFile = "data.txt";

// --- Prepare Shared State ---
var shared = new Dictionary<string, object>();
try
{
    shared["resume_text"] = File.ReadAllText(resumeFile);
}
catch (FileNotFoundException)
{
    Console.Error.WriteLine($"Error: Resume file '{resumeFile}' not found.");
    return;
}

shared["target_skills"] = targetSkills;

// --- Define and Run Flow ---
var parserNode = new ResumeParserNode(maxRetries: 3, wait: 10);
var flow = new Flow(start: parserNode);
flow.Run(shared);

// --- Display Found Skills ---
if (shared.TryGetValue("structured_data", out var rawData) && rawData is ResumeData resumeData)
{
    Console.WriteLine("\n--- Found Target Skills (from Indexes) ---");
    var foundIndexes = resumeData.SkillIndexes;
    if (foundIndexes is { Count: > 0 })
    {
        foreach (var index in foundIndexes)
        {
            if (index >= 0 && index < targetSkills.Count)
                Console.WriteLine($"- {targetSkills[index]} (Index: {index})");
            else
                Console.WriteLine($"- Warning: Found invalid skill index {index}");
        }
    }
    else
    {
        Console.WriteLine("No target skills identified from the list.");
    }
    Console.WriteLine("------------------------------------------\n");
}
