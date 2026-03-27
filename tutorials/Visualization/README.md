# PocketFlow Visualization

Interactive D3.js flow-graph visualizer for PocketFlow pipelines — implemented entirely in C#.

## Overview

This project converts any PocketFlow node/flow graph into:

1. A **Mermaid** diagram (printed to the console)
2. An **interactive D3.js** force-directed graph served over a local HTTP server

The `VisualizationUtils` helper class lives in the **SharedUtils** project so any example in
the solution can reuse it with a single method call.

## Features

| Feature | Details |
|---|---|
| Interactive graph | Nodes can be dragged to reorganise the layout |
| Group boxes | Each `Flow` / `AsyncFlow` is shown as a dashed rectangle |
| Inter-group links | Dashed arrows connect group boundaries (not node centres) |
| Action labels | Transition labels shown on every edge |
| Loop / retry edges | Bidirectional and self-referencing edges rendered with arcs |
| Built-in HTTP server | Files served via `HttpListener` — no external web server needed |
| Auto browser open | The default browser is opened automatically |

## Project Structure

```
Visualization/
├── Nodes.cs         — AsyncNode subclasses (payment, inventory, shipping)
├── Flows.cs         — OrderFlow + FlowFactory (mirrors async_flow.py)
├── LoopFlows.cs     — LoopOrderFlow with retry edges (mirrors async_loop_flow.py)
├── Program.cs       — Entry point / CLI
└── viz/             — Generated output (HTML + JSON)

SharedUtils/
└── VisualizationUtils.cs  — BuildMermaid, FlowToJson, CreateD3Visualization,
                             StartHttpServer, ServeAndOpenVisualization, VisualizeFlow
```

## Requirements

- .NET 10 SDK
- Modern web browser (Chrome, Firefox, Edge) for the visualization

## Usage

### 1. Run with default settings (standard order pipeline)

```bash
cd src/Visualization
dotnet run
```

This will:
1. Print a Mermaid diagram to the console
2. Generate `viz/order_pipeline.html` and `viz/order_pipeline.json`
3. Start a local HTTP server and open the browser automatically

### 2. Visualize the loop/retry flow

```bash
dotnet run -- --loop
```

### 3. Also execute the flow before visualizing

```bash
dotnet run -- --run
```

### 4. Generate files only (no server, no browser)

```bash
dotnet run -- --no-serve
```

### 5. Custom output directory

```bash
dotnet run -- --output-dir ./my-output
```

## Using VisualizationUtils in your own project

Add a reference to the **SharedUtils** project and call:

```csharp
using SharedUtils;

// One-liner: prints Mermaid, writes HTML/JSON, starts server, opens browser
var (htmlPath, serverThread, url) = VisualizationUtils.VisualizeFlow(
    flow:      myFlow,
    flowName:  "My Flow",
    serve:     true,
    autoOpen:  true,
    outputDir: "./viz");

// Keep the console alive while the server runs
serverThread?.Join();
```

#### Lower-level API

```csharp
// Just the Mermaid string
string mermaid = VisualizationUtils.BuildMermaid(myFlow);

// Just the JSON data model
FlowGraphData data = VisualizationUtils.FlowToJson(myFlow);

// Generate HTML + JSON files
string htmlPath = VisualizationUtils.CreateD3Visualization(data, "./viz", "my_flow", "My Flow");

// Start a file server manually
var (thread, port) = VisualizationUtils.StartHttpServer("./viz");

// Open browser
var (thread, url) = VisualizationUtils.ServeAndOpenVisualization(htmlPath);
```

## How It Works

### 1. `BuildMermaid`

Recursively walks the node graph. `Flow`/`AsyncFlow` nodes become Mermaid `subgraph`
blocks; regular nodes become labelled boxes. Already-visited nodes are linked without
re-traversal to handle loops and shared nodes.

### 2. `FlowToJson`

Produces a `FlowGraphData` record with:

- **`nodes`** — non-flow nodes with their group (flow) membership
- **`links`** — edges within a group
- **`group_links`** — edges connecting groups (inter-flow connections)
- **`flows`** — map of group ID → flow class name

Cross-group node links are promoted to group-level links in a post-processing step.

### 3. D3.js Visualization

The generated HTML embeds D3 v7 (CDN) and loads the companion JSON file.
Key simulation forces:

| Force | Purpose |
|---|---|
| `forceLink` | Keeps connected nodes at ~100 px distance |
| `forceManyBody` | Node repulsion |
| `forceCenter` | Centres the whole graph |
| `forceCollide` | Prevents node overlap |
| `groupForce` | Pulls same-group nodes toward their group centre |
| `groupLayoutForce` | Horizontally spaces linked groups |

Group boundaries are computed each tick from node positions. Inter-group arrows
connect at the exact border intersection rather than the centre.

### 4. HTTP Server

`VisualizationUtils.StartHttpServer` uses `System.Net.HttpListener` to serve the
`viz/` directory as static files on a random free port.

## Customising

### Layout parameters

Edit `VisualizationUtils.HtmlTemplate` inside `SharedUtils/VisualizationUtils.cs` to
change force strengths, link distances, node radii, colours, etc.

### Extending the data model

`FlowGraphData`, `FlowGraphNode`, and `FlowGraphLink` are plain C# records — add
properties and update the D3 JavaScript template to consume them.

## Troubleshooting

| Problem | Solution |
|---|---|
| Browser doesn't open | Run with `--no-open` and navigate manually to the URL printed in the console |
| Port conflict | A random free port is chosen automatically; no action needed |
| Empty graph | Verify that nodes are connected via `.Next(...)` or `.On(...).Then(...)` before the flow is passed to `VisualizeFlow` |
| JavaScript errors | Open the browser console; ensure the `.json` file was generated next to the `.html` file |

