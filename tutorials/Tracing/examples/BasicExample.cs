using PocketFlow;
using Tracing;

namespace Tracing.Examples;

// ── Nodes ─────────────────────────────────────────────────────────────────────

/// <summary>
/// A simple node that builds a greeting message.
/// Ported from examples/basic_example.py → <c>GreetingNode</c>.
/// </summary>
file class GreetingNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("name", out var n) ? n : "World";
    }

    protected override object? Execute(object? prepRes)
        => $"Hello, {prepRes}!";

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["greeting"] = execRes?.ToString() ?? string.Empty;
        return "default";
    }
}

/// <summary>
/// Converts the greeting stored in shared state to upper-case.
/// Ported from examples/basic_example.py → <c>UppercaseNode</c>.
/// </summary>
file class UppercaseNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return store.TryGetValue("greeting", out var g) ? g : string.Empty;
    }

    protected override object? Execute(object? prepRes)
        => prepRes?.ToString()?.ToUpperInvariant();

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        ((Dictionary<string, object>)shared)["uppercase_greeting"] = execRes?.ToString() ?? string.Empty;
        return "default";
    }
}

// ── Traced flow ───────────────────────────────────────────────────────────────

/// <summary>
/// A simple two-node flow that creates and upper-cases a greeting.
/// Inheriting from <see cref="TracedFlow"/> wires Langfuse tracing automatically.
/// Ported from examples/basic_example.py → <c>BasicGreetingFlow</c>.
/// </summary>
file class BasicGreetingFlow : TracedFlow
{
    public BasicGreetingFlow(TracingConfig? config = null)
        : base(flowName: "BasicGreetingFlow", config: config)
    {
        var greeting  = new GreetingNode();
        var uppercase = new UppercaseNode();
        greeting.Then(uppercase);
        StartNode = greeting;
    }
}

// ── Entry-point ───────────────────────────────────────────────────────────────

/// <summary>
/// Runner for the basic synchronous tracing example.
/// Ported from examples/basic_example.py → <c>main()</c>.
/// </summary>
public static class BasicExample
{
    public static void Run(TracingConfig? config = null)
    {
        Console.WriteLine("🚀 Starting PocketFlow Tracing Basic Example");
        Console.WriteLine(new string('=', 50));

        var flow   = new BasicGreetingFlow(config);
        var shared = new Dictionary<string, object> { ["name"] = "PocketFlow User" };

        Console.WriteLine($"📥 Input: name = {shared["name"]}");

        try
        {
            var result = flow.Run(shared);
            Console.WriteLine($"🎯 Result action: {result}");
            Console.WriteLine("✅ Flow completed successfully!");

            if (shared.TryGetValue("uppercase_greeting", out var greeting))
                Console.WriteLine($"🎉 Final greeting: {greeting}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"❌ Flow failed: {e.Message}");
            throw;
        }

        var host = Environment.GetEnvironmentVariable("LANGFUSE_HOST") ?? "your-langfuse-host";
        Console.WriteLine($"\n📊 Check your Langfuse dashboard: {host}");
    }
}

