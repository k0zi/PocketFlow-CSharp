using DocumentUpdater;
using PocketFlow;

// ── Default paths (mirrors update_pocketflow_mdc.py) ─────────────────────────
// Defaults assume the tool is run from the PocketFlow repository root.

var defaultDocsDir  = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", "..", "docs");
var defaultRulesDir = Path.Combine(Directory.GetCurrentDirectory(), ".cursor", "rules");

// ── Argument parsing ──────────────────────────────────────────────────────────

string docsDir  = defaultDocsDir;
string rulesDir = defaultRulesDir;

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--docs-dir":  docsDir  = args[++i]; break;
        case "--rules-dir": rulesDir = args[++i]; break;
        case "-h":
        case "--help":
            PrintHelp();
            return 0;
        default:
            Console.Error.WriteLine($"Unknown argument: {args[i]}");
            PrintHelp();
            return 1;
    }
}

// ── Shared store ──────────────────────────────────────────────────────────────

var shared = new Dictionary<string, object?>
{
    ["docs_dir"]       = docsDir,
    ["rules_dir"]      = rulesDir,
    ["md_files"]       = new List<string>(),
    ["success_count"]  = 0,
    ["failure_count"]  = 0,
};

// ── Build flow ────────────────────────────────────────────────────────────────
//
//   DiscoverFilesNode → CreateCombinedGuideNode → ConvertMdBatchNode

var discoverFiles = new DiscoverFilesNode();
var createGuide   = new CreateCombinedGuideNode();
var convertFiles  = new ConvertMdBatchNode();

discoverFiles.Then(createGuide).Then(convertFiles);

var flow = new Flow(start: discoverFiles);

// ── Run ───────────────────────────────────────────────────────────────────────

try
{
    flow.Run(shared);
    var failures = (int)(shared["failure_count"] ?? 0);
    return failures == 0 ? 0 : 1;
}
catch (Exception e)
{
    Console.Error.WriteLine($"Error: {e.Message}");
    return 1;
}

// ── Help ──────────────────────────────────────────────────────────────────────

static void PrintHelp()
{
    Console.WriteLine("""
        Usage: DocumentUpdater [options]

        Options:
          --docs-dir  <path>   Path to the PocketFlow docs directory
                               (default: ./docs relative to working directory)
          --rules-dir <path>   Output directory for generated .mdc files
                               (default: ./.cursor/rules relative to working directory)
          -h, --help           Print this help message

        The tool should be run from the PocketFlow repository root, e.g.:
          dotnet run --project src/DocumentUpdater
        """);
}
