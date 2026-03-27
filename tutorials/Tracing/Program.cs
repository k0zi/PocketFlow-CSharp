using Tracing;
using Tracing.Examples;

Console.WriteLine("🚀 PocketFlow Tracing — C# / Langfuse");
Console.WriteLine(new string('=', 50));

// ── Configuration ─────────────────────────────────────────────────────────────
// Reads LANGFUSE_SECRET_KEY, LANGFUSE_PUBLIC_KEY, LANGFUSE_HOST + optional
// POCKETFLOW_* variables from the current environment.
var config = TracingConfig.FromEnv();

if (!config.Validate())
{
    Console.WriteLine();
    Console.WriteLine("⚠️  Langfuse credentials not configured — tracing will run in no-op mode.");
    Console.WriteLine("   Set these environment variables to enable Langfuse integration:");
    Console.WriteLine("     LANGFUSE_SECRET_KEY=sk-...");
    Console.WriteLine("     LANGFUSE_PUBLIC_KEY=pk-...");
    Console.WriteLine("     LANGFUSE_HOST=https://your-langfuse-host");
    Console.WriteLine();
}

// ── Basic synchronous example ─────────────────────────────────────────────────
Console.WriteLine("--- Basic Example ---");
BasicExample.Run(config);
Console.WriteLine();

// ── Async example ─────────────────────────────────────────────────────────────
Console.WriteLine("--- Async Example ---");
await AsyncExample.RunAsync(config);
