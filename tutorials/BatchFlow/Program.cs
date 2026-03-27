using PocketFlow;

// C# port of main.py + flow.py from the pocketflow-batch-flow cookbook.
// Applies grayscale, blur, and sepia filters to multiple images using BatchFlow.

Console.WriteLine("Processing images with filters...");

// --- Build the base Flow (single image pipeline) ---
var load   = new LoadImageNode();
var filter = new ApplyFilterNode();
var save   = new SaveImageNode();

load.On("apply_filter").Then(filter);
filter.On("save").Then(save);

var baseFlow = new Flow(start: load);

// --- Wrap in BatchFlow to process all image × filter combinations ---
var batchFlow = new ImageBatchFlow(start: baseFlow);
batchFlow.Run(new Dictionary<string, object>());

Console.WriteLine("\nAll images processed successfully!");
Console.WriteLine("Check the 'output' directory for results.");

