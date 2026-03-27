using PocketFlow;

// C# port of main.py + flow.py from the pocketflow-flow cookbook.
// Builds an interactive text converter with a branching flow loop.

Console.WriteLine("\nWelcome to Text Converter!");
Console.WriteLine("=========================");

// ── Nodes ────────────────────────────────────────────────────────────────────
var textInput     = new TextInputNode();
var textTransform = new TextTransformNode();
var endNode       = new EndNode();

// ── Connections (mirrors flow.py) ────────────────────────────────────────────
textInput.On("transform").Then(textTransform);  // user chose a transformation
textInput.On("exit").Then(endNode);             // user chose Exit from menu

textTransform.On("input").Then(textInput);      // convert another text
textTransform.On("exit").Then(endNode);         // user declined another round

// ── Run ──────────────────────────────────────────────────────────────────────
var flow = new Flow(start: textInput);
flow.Run(new Dictionary<string, object>());

Console.WriteLine("\nThank you for using Text Converter!");

