using PocketFlow;
using SharedUtils;
using Visualization;

// ── Parse arguments ───────────────────────────────────────────────────────────

bool   noServe   = args.Contains("--no-serve");
bool   noOpen    = args.Contains("--no-open");
bool   runLoop   = args.Contains("--loop");
string outputDir = "./viz";

var outIdx = Array.IndexOf(args, "--output-dir");
if (outIdx >= 0 && outIdx + 1 < args.Length)
    outputDir = args[outIdx + 1];

// ── Build the selected flow ───────────────────────────────────────────────────

Console.WriteLine("PocketFlow Visualization Demo");
Console.WriteLine("==============================\n");

if (runLoop)
{
    Console.WriteLine("Using: Loop Order Pipeline (retry/loop edges)");
    var loopFlow = LoopFlowFactory.BuildLoopOrderPipeline();

    var (htmlPath, serverThread, url) = VisualizationUtils.VisualizeFlow(
        flow:      loopFlow,
        flowName:  "Loop Order Pipeline",
        serve:     !noServe,
        autoOpen:  !noOpen,
        outputDir: outputDir);

    if (!noServe && serverThread != null)
    {
        Console.WriteLine("\nServer is running. Press Ctrl+C to stop...");
        try { serverThread.Join(); }
        catch (ThreadInterruptedException) { }
    }
}
else
{
    Console.WriteLine("Using: Standard Order Pipeline");
    var orderFlow = FlowFactory.BuildOrderPipeline();

    // Optionally run the flow to verify it works
    if (args.Contains("--run"))
    {
        Console.WriteLine("\n--- Running the flow ---");
        var shared = FlowFactory.BuildSharedData();
        await orderFlow.RunAsync(shared);

        Console.WriteLine("\nOrder processing completed!");
        Console.WriteLine($"  Payment:   {shared.GetValueOrDefault("payment_confirmation", "N/A")}");
        Console.WriteLine($"  Inventory: {shared.GetValueOrDefault("inventory_update",     "N/A")}");
        Console.WriteLine($"  Shipping:  {shared.GetValueOrDefault("pickup_status",        "N/A")}");
    }

    var (htmlPath, serverThread, url) = VisualizationUtils.VisualizeFlow(
        flow:      orderFlow,
        flowName:  "Order Pipeline",
        serve:     !noServe,
        autoOpen:  !noOpen,
        outputDir: outputDir);

    if (!noServe && serverThread != null)
    {
        Console.WriteLine("\nServer is running. Press Ctrl+C to stop...");
        try { serverThread.Join(); }
        catch (ThreadInterruptedException) { }
    }
}
