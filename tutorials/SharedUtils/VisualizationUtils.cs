using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using PocketFlow;

namespace SharedUtils;

// ── Data Models ───────────────────────────────────────────────────────────────

public record FlowGraphNode(
    [property: JsonPropertyName("id")] int Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("group")] int Group);

public record FlowGraphLink(
    [property: JsonPropertyName("source")] int Source,
    [property: JsonPropertyName("target")] int Target,
    [property: JsonPropertyName("action")] string Action);

public record FlowGraphData(
    [property: JsonPropertyName("nodes")] List<FlowGraphNode> Nodes,
    [property: JsonPropertyName("links")] List<FlowGraphLink> Links,
    [property: JsonPropertyName("group_links")] List<FlowGraphLink> GroupLinks,
    [property: JsonPropertyName("flows")] Dictionary<string, string> Flows);

// ── VisualizationUtils ────────────────────────────────────────────────────────

/// <summary>
/// Utility methods for generating Mermaid diagrams and interactive D3.js
/// visualizations from PocketFlow node/flow graphs.
/// </summary>
public static class VisualizationUtils
{
    // ── Flow helpers ──────────────────────────────────────────────────────────

    private static bool IsFlowLike(BaseNode node) =>
        node is Flow || node is AsyncFlow;

    private static BaseNode? GetStartNode(BaseNode node) =>
        node is Flow f  ? f.StartNode  :
        node is AsyncFlow af ? af.StartNode : null;

    // ── BuildMermaid ──────────────────────────────────────────────────────────

    /// <summary>
    /// Build a Mermaid LR graph diagram string from a PocketFlow node/flow.
    /// </summary>
    public static string BuildMermaid(BaseNode start)
    {
        var ids     = new Dictionary<BaseNode, string>(ReferenceEqualityComparer.Instance);
        var visited = new HashSet<BaseNode>(ReferenceEqualityComparer.Instance);
        var lines   = new List<string> { "graph LR" };
        int ctr     = 1;

        string GetId(BaseNode n)
        {
            if (!ids.TryGetValue(n, out var id))
                ids[n] = id = $"N{ctr++}";
            return id;
        }

        void Link(string a, string b, string? action = null)
        {
            lines.Add(action != null
                ? $"    {a} -->|{action}| {b}"
                : $"    {a} --> {b}");
        }

        void Walk(BaseNode node, string? parent = null, string? action = null)
        {
            if (visited.Contains(node))
            {
                if (parent != null) Link(parent, GetId(node), action);
                return;
            }
            visited.Add(node);

            if (IsFlowLike(node))
            {
                var startNode = GetStartNode(node);
                if (startNode != null && parent != null)
                    Link(parent, GetId(startNode), action);

                lines.Add($"\n    subgraph sub_flow_{GetId(node)}[{node.GetType().Name}]");
                if (startNode != null) Walk(startNode);

                foreach (var (act, nxt) in node.Successors)
                {
                    if (startNode != null)
                        Walk(nxt, GetId(startNode), act);
                    else if (parent != null)
                        Link(parent, GetId(nxt), action);
                    else
                        Walk(nxt, null, act);
                }
                lines.Add("    end\n");
            }
            else
            {
                var nid = GetId(node);
                lines.Add($"    {nid}['{node.GetType().Name}']");
                if (parent != null) Link(parent, nid, action);
                foreach (var (act, nxt) in node.Successors)
                    Walk(nxt, nid, act);
            }
        }

        Walk(start);
        return string.Join("\n", lines);
    }

    // ── FlowToJson ────────────────────────────────────────────────────────────

    /// <summary>
    /// Convert a PocketFlow node/flow to a <see cref="FlowGraphData"/> structure
    /// suitable for D3.js visualisation.
    /// </summary>
    public static FlowGraphData FlowToJson(BaseNode start)
    {
        var nodes      = new List<FlowGraphNode>();
        var links      = new List<FlowGraphLink>();
        var groupLinks = new List<FlowGraphLink>();
        var ids        = new Dictionary<BaseNode, int>(ReferenceEqualityComparer.Instance);
        var nodeTypes  = new Dictionary<int, string>();
        var flowNodes  = new Dictionary<int, BaseNode>();
        var visited    = new HashSet<(int, string?)>();
        int ctr        = 1;

        int GetId(BaseNode n)
        {
            if (!ids.TryGetValue(n, out var id))
            {
                id = ctr++;
                ids[n]       = id;
                nodeTypes[id] = n.GetType().Name;
                if (IsFlowLike(n)) flowNodes[id] = n;
            }
            return id;
        }

        void Walk(BaseNode node, int? parent = null, int? group = null,
                  int? parentGroup = null, string? action = null)
        {
            int nodeId = GetId(node);
            var key    = (nodeId, action);
            if (visited.Contains(key)) return;
            visited.Add(key);

            // Register regular (non-flow) nodes in the nodes list
            if (!IsFlowLike(node) && !nodes.Any(n => n.Id == nodeId))
                nodes.Add(new FlowGraphNode(nodeId, nodeTypes[nodeId], group ?? 0));

            // Add link from parent to this node
            if (parent.HasValue && !IsFlowLike(node))
                links.Add(new FlowGraphLink(parent.Value, nodeId, action ?? "default"));

            if (IsFlowLike(node))
            {
                int flowGroup = nodeId;

                // Cross-flow group link
                if (parentGroup.HasValue && parentGroup.Value != flowGroup &&
                    !groupLinks.Any(l => l.Source == parentGroup.Value && l.Target == flowGroup))
                    groupLinks.Add(new FlowGraphLink(parentGroup.Value, flowGroup, action ?? "default"));

                var startNode = GetStartNode(node);
                if (startNode != null)
                {
                    Walk(startNode, parent, flowGroup, parentGroup, action);
                    foreach (var (nextAction, nxt) in node.Successors)
                        Walk(nxt, GetId(startNode), flowGroup, parentGroup, nextAction);
                }
            }
            else
            {
                foreach (var (nextAction, nxt) in node.Successors)
                {
                    if (IsFlowLike(nxt))
                        Walk(nxt, nodeId, null, group, nextAction);
                    else
                        Walk(nxt, nodeId, group, parentGroup, nextAction);
                }
            }
        }

        Walk(start);

        // Post-process: replace cross-group node links with group-level links
        var nodeGroups   = nodes.ToDictionary(n => n.Id, n => n.Group);
        var filteredLinks = new List<FlowGraphLink>();

        foreach (var link in links)
        {
            var sg = nodeGroups.GetValueOrDefault(link.Source, 0);
            var tg = nodeGroups.GetValueOrDefault(link.Target, 0);

            if (sg != tg && sg > 0 && tg > 0)
            {
                if (!groupLinks.Any(gl => gl.Source == sg && gl.Target == tg))
                    groupLinks.Add(new FlowGraphLink(sg, tg, link.Action));
            }
            else
            {
                filteredLinks.Add(link);
            }
        }

        return new FlowGraphData(
            nodes,
            filteredLinks,
            groupLinks,
            flowNodes.ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.GetType().Name));
    }

    // ── CreateD3Visualization ─────────────────────────────────────────────────

    /// <summary>
    /// Serialise <paramref name="data"/> to JSON and generate an HTML file with an
    /// interactive D3.js force-directed graph.
    /// </summary>
    /// <returns>Absolute path to the generated HTML file.</returns>
    public static string CreateD3Visualization(
        FlowGraphData data,
        string outputDir  = "./viz",
        string filename   = "flow_viz",
        string htmlTitle  = "PocketFlow Visualization")
    {
        Directory.CreateDirectory(outputDir);

        // --- JSON ---
        var jsonPath = Path.Combine(outputDir, $"{filename}.json");
        var jsonOpts = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(data, jsonOpts));

        // --- HTML ---
        var html = HtmlTemplate
            .Replace("FILENAME_PLACEHOLDER", filename)
            .Replace("TITLE_PLACEHOLDER",    htmlTitle);

        var htmlPath = Path.Combine(outputDir, $"{filename}.html");
        File.WriteAllText(htmlPath, html);

        Console.WriteLine($"Visualization created at {Path.GetFullPath(htmlPath)}");
        return Path.GetFullPath(htmlPath);
    }

    // ── HTTP server ───────────────────────────────────────────────────────────

    /// <summary>Find a free TCP port on localhost.</summary>
    public static int FindFreePort()
    {
        using var s = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
        s.Start();
        int port = ((IPEndPoint)s.LocalEndpoint).Port;
        s.Stop();
        return port;
    }

    /// <summary>
    /// Start a simple static-file HTTP server rooted at <paramref name="directory"/>.
    /// </summary>
    /// <returns>(<see cref="Thread"/> background thread, port number)</returns>
    public static (Thread Thread, int Port) StartHttpServer(string directory, int? port = null)
    {
        port ??= FindFreePort();
        var root     = Path.GetFullPath(directory);
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        var thread = new Thread(() =>
        {
            while (listener.IsListening)
            {
                try
                {
                    var ctx  = listener.GetContext();
                    var req  = ctx.Request;
                    var resp = ctx.Response;

                    var localPath = req.Url?.LocalPath.TrimStart('/') ?? string.Empty;
                    if (localPath == string.Empty) localPath = "index.html";

                    var filePath = Path.Combine(root, localPath);

                    if (File.Exists(filePath))
                    {
                        resp.ContentType = Path.GetExtension(filePath).ToLowerInvariant() switch
                        {
                            ".html" => "text/html; charset=utf-8",
                            ".json" => "application/json; charset=utf-8",
                            ".js"   => "application/javascript",
                            ".css"  => "text/css",
                            _       => "application/octet-stream"
                        };
                        var bytes = File.ReadAllBytes(filePath);
                        resp.ContentLength64 = bytes.Length;
                        resp.OutputStream.Write(bytes);
                    }
                    else
                    {
                        resp.StatusCode = 404;
                    }
                    resp.OutputStream.Close();
                }
                catch { /* server stopped or connection reset */ }
            }
        })
        { IsBackground = true };

        thread.Start();
        Console.WriteLine($"Server started at http://localhost:{port}");
        return (thread, port.Value);
    }

    /// <summary>
    /// Serve the generated HTML file over HTTP and optionally open it in the
    /// default browser.
    /// </summary>
    /// <returns>(<see cref="Thread"/> background thread, URL string)</returns>
    public static (Thread Thread, string Url) ServeAndOpenVisualization(
        string htmlPath, bool autoOpen = true)
    {
        var dir      = Path.GetDirectoryName(Path.GetFullPath(htmlPath))!;
        var file     = Path.GetFileName(htmlPath);
        var (thread, port) = StartHttpServer(dir);
        var url      = $"http://localhost:{port}/{file}";

        if (autoOpen)
        {
            Console.WriteLine($"Opening {url} in your browser...");
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else
        {
            Console.WriteLine($"Visualization available at {url}");
        }

        return (thread, url);
    }

    // ── VisualizeFlow ─────────────────────────────────────────────────────────

    /// <summary>
    /// High-level helper: print a Mermaid diagram, generate D3.js HTML, and
    /// optionally start a browser preview.
    /// </summary>
    /// <returns>
    /// When <paramref name="serve"/> is <c>true</c>: absolute HTML path, server
    /// <see cref="Thread"/>, and URL.  Otherwise just the HTML path.
    /// </returns>
    public static (string HtmlPath, Thread? ServerThread, string? Url) VisualizeFlow(
        BaseNode flow,
        string   flowName,
        bool     serve      = true,
        bool     autoOpen   = true,
        string   outputDir  = "./viz",
        string?  htmlTitle  = null)
    {
        Console.WriteLine($"\n--- {flowName} Mermaid Diagram ---");
        Console.WriteLine(BuildMermaid(flow));

        Console.WriteLine($"\n--- {flowName} D3.js Visualization ---");
        var jsonData = FlowToJson(flow);

        var safeFilename = flowName.ToLowerInvariant().Replace(' ', '_');
        htmlTitle ??= $"PocketFlow: {flowName}";

        var htmlPath = CreateD3Visualization(jsonData, outputDir, safeFilename, htmlTitle);

        if (serve)
        {
            var (thread, url) = ServeAndOpenVisualization(htmlPath, autoOpen);
            return (htmlPath, thread, url);
        }

        return (htmlPath, null, null);
    }

    // ── HTML template ─────────────────────────────────────────────────────────

    private const string HtmlTemplate = """
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8">
    <title>TITLE_PLACEHOLDER</title>
    <script src="https://d3js.org/d3.v7.min.js"></script>
    <style>
        body {
            font-family: Arial, sans-serif;
            margin: 0;
            padding: 0;
            overflow: hidden;
        }
        svg {
            width: 100vw;
            height: 100vh;
        }
        .links path {
            fill: none;
            stroke: #999;
            stroke-opacity: 0.6;
            stroke-width: 1.5px;
        }
        .group-links path {
            fill: none;
            stroke: #333;
            stroke-opacity: 0.8;
            stroke-width: 2px;
            stroke-dasharray: 5,5;
        }
        .nodes circle {
            stroke: #fff;
            stroke-width: 1.5px;
        }
        .node-labels {
            font-size: 12px;
            pointer-events: none;
        }
        .link-labels {
            font-size: 10px;
            fill: #666;
            pointer-events: none;
        }
        .group-link-labels {
            font-size: 11px;
            font-weight: bold;
            fill: #333;
            pointer-events: none;
        }
        .group-container {
            stroke: #333;
            stroke-width: 1.5px;
            stroke-dasharray: 5,5;
            fill: rgba(200, 200, 200, 0.1);
            rx: 10;
            ry: 10;
        }
        .group-label {
            font-size: 14px;
            font-weight: bold;
            pointer-events: none;
        }
    </style>
</head>
<body>
    <svg id="graph"></svg>
    <script>
        d3.json("FILENAME_PLACEHOLDER.json").then(data => {
            const svg = d3.select("#graph");
            const width = window.innerWidth;
            const height = window.innerHeight;

            svg.append("defs").append("marker")
                .attr("id", "arrowhead")
                .attr("viewBox", "0 -5 10 10")
                .attr("refX", 25)
                .attr("refY", 0)
                .attr("orient", "auto")
                .attr("markerWidth", 6)
                .attr("markerHeight", 6)
                .attr("xoverflow", "visible")
                .append("path")
                .attr("d", "M 0,-5 L 10,0 L 0,5")
                .attr("fill", "#999");

            svg.append("defs").append("marker")
                .attr("id", "group-arrowhead")
                .attr("viewBox", "0 -5 10 10")
                .attr("refX", 3)
                .attr("refY", 0)
                .attr("orient", "auto")
                .attr("markerWidth", 8)
                .attr("markerHeight", 8)
                .attr("xoverflow", "visible")
                .append("path")
                .attr("d", "M 0,-5 L 10,0 L 0,5")
                .attr("fill", "#333");

            const color = d3.scaleOrdinal(d3.schemeCategory10);

            const groups = {};
            data.nodes.forEach(node => {
                if (node.group > 0) {
                    if (!groups[node.group]) {
                        const flowName = data.flows && data.flows[node.group]
                            ? data.flows[node.group]
                            : `Flow ${node.group}`;
                        groups[node.group] = {
                            id: node.group,
                            name: flowName,
                            nodes: [],
                            x: 0, y: 0, width: 0, height: 0
                        };
                    }
                    groups[node.group].nodes.push(node);
                }
            });

            const simulation = d3.forceSimulation(data.nodes)
                .force("link", d3.forceLink(data.links).id(d => d.id).distance(100))
                .force("charge", d3.forceManyBody().strength(-30))
                .force("center", d3.forceCenter(width / 2, height / 2))
                .force("collide", d3.forceCollide().radius(50));

            const groupForce = alpha => {
                for (let i = 0; i < data.nodes.length; i++) {
                    const node = data.nodes[i];
                    if (node.group > 0) {
                        const group = groups[node.group];
                        if (group && group.nodes.length > 1) {
                            let centerX = 0, centerY = 0;
                            group.nodes.forEach(n => {
                                centerX += n.x || 0;
                                centerY += n.y || 0;
                            });
                            centerX /= group.nodes.length;
                            centerY /= group.nodes.length;
                            const k = alpha * 0.3;
                            node.vx += (centerX - node.x) * k;
                            node.vy += (centerY - node.y) * k;
                        }
                    }
                }
            };

            const groupLayoutForce = alpha => {
                const groupCenters = Object.values(groups).map(g => ({ id: g.id, cx: 0, cy: 0 }));

                Object.values(groups).forEach(g => {
                    if (g.nodes.length > 0) {
                        let cx = 0, cy = 0;
                        g.nodes.forEach(n => { cx += n.x || 0; cy += n.y || 0; });
                        const gc = groupCenters.find(c => c.id === g.id);
                        if (gc) { gc.cx = cx / g.nodes.length; gc.cy = cy / g.nodes.length; }
                    }
                });

                const k = alpha * 0.05;
                for (let i = 0; i < data.group_links.length; i++) {
                    const link = data.group_links[i];
                    const source = groupCenters.find(g => g.id === link.source);
                    const target = groupCenters.find(g => g.id === link.target);
                    if (source && target) {
                        const desiredDx = 300;
                        const dx = target.cx - source.cx;
                        const diff = desiredDx - Math.abs(dx);
                        groups[source.id].nodes.forEach(n => { n.vx += (dx > 0 ? -diff : diff) * k; });
                        groups[target.id].nodes.forEach(n => { n.vx += (dx > 0 ?  diff : -diff) * k; });
                    }
                }
            };

            simulation.force("group", groupForce);
            simulation.force("groupLayout", groupLayoutForce);

            const link = svg.append("g")
                .attr("class", "links")
                .selectAll("path")
                .data(data.links)
                .enter().append("path")
                .attr("stroke-width", 2)
                .attr("stroke", "#999")
                .attr("marker-end", "url(#arrowhead)");

            const groupContainers = svg.append("g")
                .attr("class", "groups")
                .selectAll("rect")
                .data(Object.values(groups))
                .enter().append("rect")
                .attr("class", "group-container")
                .attr("fill", d => d3.color(color(d.id)).copy({opacity: 0.2}));

            const groupLink = svg.append("g")
                .attr("class", "group-links")
                .selectAll("path")
                .data(data.group_links || [])
                .enter().append("path")
                .attr("stroke-width", 2)
                .attr("stroke", "#333")
                .attr("marker-end", "url(#group-arrowhead)");

            const groupLinkLabel = svg.append("g")
                .attr("class", "group-link-labels")
                .selectAll("text")
                .data(data.group_links || [])
                .enter().append("text")
                .text(d => d.action)
                .attr("font-size", "11px")
                .attr("font-weight", "bold")
                .attr("fill", "#333");

            const groupLabels = svg.append("g")
                .attr("class", "group-labels")
                .selectAll("text")
                .data(Object.values(groups))
                .enter().append("text")
                .attr("class", "group-label")
                .text(d => d.name)
                .attr("fill", d => d3.color(color(d.id)).darker());

            const linkLabel = svg.append("g")
                .attr("class", "link-labels")
                .selectAll("text")
                .data(data.links)
                .enter().append("text")
                .text(d => d.action)
                .attr("font-size", "10px")
                .attr("fill", "#666");

            const node = svg.append("g")
                .attr("class", "nodes")
                .selectAll("circle")
                .data(data.nodes)
                .enter().append("circle")
                .attr("r", 15)
                .attr("fill", d => color(d.group))
                .call(d3.drag()
                    .on("start", dragstarted)
                    .on("drag", dragged)
                    .on("end", dragended));

            const nodeLabel = svg.append("g")
                .attr("class", "node-labels")
                .selectAll("text")
                .data(data.nodes)
                .enter().append("text")
                .text(d => d.name)
                .attr("text-anchor", "middle")
                .attr("dy", 25);

            node.append("title").text(d => d.name);

            simulation.on("tick", () => {
                link.attr("d", d => {
                    if (d.source === d.target) {
                        const nx = d.source.x, ny = d.source.y;
                        return `M ${nx},${ny - 5} C ${nx + 50},${ny - 30} ${nx + 40},${ny + 10} ${nx},${ny}`;
                    }
                    const isReverse = data.links.some(l => l.source === d.target && l.target === d.source);
                    if (isReverse) {
                        const dx = d.target.x - d.source.x, dy = d.target.y - d.source.y;
                        const dr = Math.sqrt(dx * dx + dy * dy) * 0.9;
                        return `M${d.source.x},${d.source.y}A${dr},${dr} 0 0,1 ${d.target.x},${d.target.y}`;
                    }
                    return `M${d.source.x},${d.source.y} L${d.target.x},${d.target.y}`;
                });

                node.attr("cx", d => d.x).attr("cy", d => d.y);
                nodeLabel.attr("x", d => d.x).attr("y", d => d.y);

                linkLabel
                    .attr("x", d => {
                        if (d.source === d.target) return d.source.x + 30;
                        const rev = data.links.find(l => l.source === d.target && l.target === d.source);
                        if (rev) {
                            const dx = d.target.x - d.source.x, dy = d.target.y - d.source.y;
                            const len = Math.sqrt(dx * dx + dy * dy);
                            return (d.source.x + d.target.x) / 2 + (-dy / len * 10);
                        }
                        return (d.source.x + d.target.x) / 2;
                    })
                    .attr("y", d => {
                        if (d.source === d.target) return d.source.y;
                        const rev = data.links.find(l => l.source === d.target && l.target === d.source);
                        if (rev) {
                            const dx = d.target.x - d.source.x, dy = d.target.y - d.source.y;
                            const len = Math.sqrt(dx * dx + dy * dy);
                            return (d.source.y + d.target.y) / 2 + (dx / len * 10);
                        }
                        return (d.source.y + d.target.y) / 2;
                    });

                groupContainers.each(function(d) {
                    if (d.nodes.length > 0) {
                        let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
                        d.nodes.forEach(n => {
                            minX = Math.min(minX, n.x - 30); minY = Math.min(minY, n.y - 30);
                            maxX = Math.max(maxX, n.x + 30); maxY = Math.max(maxY, n.y + 40);
                        });
                        const pad = 20;
                        minX -= pad; minY -= pad; maxX += pad; maxY += pad;
                        d.x = minX; d.y = minY;
                        d.width = maxX - minX; d.height = maxY - minY;
                        d.centerX = minX + d.width / 2; d.centerY = minY + d.height / 2;
                        d3.select(this)
                            .attr("x", minX).attr("y", minY)
                            .attr("width", d.width).attr("height", d.height);
                        groupLabels.filter(g => g.id === d.id)
                            .attr("x", minX + 10).attr("y", minY + 20);
                    }
                });

                groupLink.attr("d", d => {
                    const sg = groups[d.source], tg = groups[d.target];
                    if (!sg || !tg) return "";
                    const sx = sg.centerX, sy = sg.centerY, tx = tg.centerX, ty = tg.centerY;
                    const angle = Math.atan2(ty - sy, tx - sx);
                    const cosA = Math.cos(angle), sinA = Math.sin(angle);
                    const ts = [
                        sinA !== 0 ? (sg.y - sy) / sinA : Infinity,
                        sinA !== 0 ? (sg.y + sg.height - sy) / sinA : Infinity,
                        cosA !== 0 ? (sg.x - sx) / cosA : Infinity,
                        cosA !== 0 ? (sg.x + sg.width - sx) / cosA : Infinity
                    ].filter(t => t > 0);
                    const t_src = ts.length ? Math.min(...ts) : Infinity;
                    const oppA = angle + Math.PI;
                    const cosO = Math.cos(oppA), sinO = Math.sin(oppA);
                    const tt = [
                        sinO !== 0 ? (tg.y - ty) / sinO : Infinity,
                        sinO !== 0 ? (tg.y + tg.height - ty) / sinO : Infinity,
                        cosO !== 0 ? (tg.x - tx) / cosO : Infinity,
                        cosO !== 0 ? (tg.x + tg.width - tx) / cosO : Infinity
                    ].filter(t => t > 0);
                    const t_tgt = tt.length ? Math.min(...tt) : Infinity;
                    const srcX = t_src !== Infinity ? sx + cosA * t_src : sx;
                    const srcY = t_src !== Infinity ? sy + sinA * t_src : sy;
                    const tgtX = t_tgt !== Infinity ? tx + cosO * t_tgt : tx;
                    const tgtY = t_tgt !== Infinity ? ty + sinO * t_tgt : ty;
                    return `M${srcX},${srcY} L${tgtX},${tgtY}`;
                });

                groupLinkLabel
                    .attr("x", d => {
                        const sg = groups[d.source], tg = groups[d.target];
                        return sg && tg ? (sg.centerX + tg.centerX) / 2 : 0;
                    })
                    .attr("y", d => {
                        const sg = groups[d.source], tg = groups[d.target];
                        return sg && tg ? (sg.centerY + tg.centerY) / 2 - 10 : 0;
                    });
            });

            function dragstarted(event, d) {
                if (!event.active) simulation.alphaTarget(0.3).restart();
                d.fx = d.x; d.fy = d.y;
            }
            function dragged(event, d) { d.fx = event.x; d.fy = event.y; }
            function dragended(event, d) {
                if (!event.active) simulation.alphaTarget(0);
                d.fx = null; d.fy = null;
            }
        });
    </script>
</body>
</html>
""";
}

