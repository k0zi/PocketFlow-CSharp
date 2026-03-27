using PocketFlow;

// ── Build the flow ────────────────────────────────────────────────────────────

var streamNode = new StreamNode();
var flow       = new AsyncFlow(start: streamNode);

// ── Run ───────────────────────────────────────────────────────────────────────

var shared = new Dictionary<string, object>
{
    ["prompt"] = "What's the meaning of life?"
};

await flow.RunAsync(shared);
