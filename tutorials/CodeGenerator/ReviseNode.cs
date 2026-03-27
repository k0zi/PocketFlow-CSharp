using System.Text;
using PocketFlow;

namespace CodeGenerator;

// Mirrors nodes.py::Revise
// Prompts the LLM to diagnose failures and output revised test cases and/or
// a revised function.  Updates shared state accordingly.
public class ReviseNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store       = (Dictionary<string, object>)shared;
        var testResults = (List<Dictionary<string, object?>>)store["test_results"];
        var failedTests = testResults.Where(r => !(bool)r["passed"]!).ToList();

        return new Dictionary<string, object>
        {
            ["problem"]       = store["problem"],
            ["test_cases"]    = store["test_cases"],
            ["function_code"] = store["function_code"],
            ["failed_tests"]  = (object)failedTests
        };
    }

    protected override object? Execute(object? prepRes)
    {
        var inputs      = (Dictionary<string, object>)prepRes!;
        var testCases   = (List<object>)inputs["test_cases"];
        var failedTests = (List<Dictionary<string, object?>>)inputs["failed_tests"];

        // ── Format current test cases ─────────────────────────────────────
        var sbTests = new StringBuilder();
        for (var i = 0; i < testCases.Count; i++)
        {
            var tc = (Dictionary<object, object>)testCases[i];
            sbTests.AppendLine($"{i + 1}. {tc["name"]}");
            sbTests.AppendLine($"   input:    {tc["input"]}");
            sbTests.AppendLine($"   expected: {tc["expected"]}");
        }

        // ── Format failed tests ───────────────────────────────────────────
        var sbFailed = new StringBuilder();
        for (var i = 0; i < failedTests.Count; i++)
        {
            var r  = failedTests[i];
            var tc = (Dictionary<object, object>)r["test_case"]!;
            sbFailed.AppendLine($"{i + 1}. {tc["name"]}:");
            sbFailed.AppendLine(r["error"] is not null
                ? $"   error:    {r["error"]}"
                : $"   actual:   {r["actual"]}");
            sbFailed.AppendLine($"   expected: {r["expected"]}");
        }

        var prompt = $$"""
Problem: {{inputs["problem"]}}

Current test cases:
{{sbTests}}
Current C# function:
```csharp
{{inputs["function_code"]}}
```

Failed tests:
{{sbFailed}}
Analyse the failures and output revisions in YAML.
You may revise test cases (if the expected output was wrong), the function code (if the logic is wrong), or both.

IMPORTANT:
- test_cases is a dictionary mapping 1-based integer keys to revised test case entries.
- function_code must be a method named "RunCode" declared as public static object RunCode(...).
- Do NOT include using statements, a namespace, or a class declaration.

```yaml
reasoning: |
    Looking at the failures I see that...
    I will revise...
test_cases:
  1:
    name: "Revised test name"
    input: {...}
    expected: ...
function_code: |
  public static object RunCode(...)
  {
      return ...;
  }
```
""";

        var response = OllamaConnector.CallLlm(prompt);
        var result   = YamlHelper.ParseBlock(response);

        // ── Validate test case revisions ──────────────────────────────────
        if (result.ContainsKey("test_cases"))
        {
            var revisions = (Dictionary<object, object>)result["test_cases"];
            foreach (var kvp in revisions)
            {
                var tc = (Dictionary<object, object>)kvp.Value;
                if (!tc.ContainsKey("name"))
                    throw new InvalidOperationException($"Revision {kvp.Key} missing 'name'");
                if (!tc.ContainsKey("input"))
                    throw new InvalidOperationException($"Revision {kvp.Key} missing 'input'");
                if (!tc.ContainsKey("expected"))
                    throw new InvalidOperationException($"Revision {kvp.Key} missing 'expected'");
            }
        }

        // ── Validate function code ────────────────────────────────────────
        if (result.ContainsKey("function_code"))
        {
            var code = result["function_code"]?.ToString() ?? string.Empty;
            if (!code.Contains("RunCode"))
                throw new InvalidOperationException(
                    "Revised function does not contain 'RunCode'");
        }

        return result;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store     = (Dictionary<string, object>)shared;
        var revision  = (Dictionary<object, object>)execRes!;
        var iteration = (int)store["iteration_count"];

        Console.WriteLine($"\n=== Revisions (Iteration {iteration}) ===");

        // ── Apply test-case revisions ─────────────────────────────────────
        if (revision.ContainsKey("test_cases"))
        {
            var revisions    = (Dictionary<object, object>)revision["test_cases"];
            var currentTests = ((List<object>)store["test_cases"]).ToList();

            Console.WriteLine("Revising test cases:");
            foreach (var kvp in revisions)
            {
                var index   = Convert.ToInt32(kvp.Key) - 1; // 1-based → 0-based
                var revised = (Dictionary<object, object>)kvp.Value;

                if (index < 0 || index >= currentTests.Count) continue;

                var old = (Dictionary<object, object>)currentTests[index];
                Console.WriteLine($"  Test {kvp.Key}: '{old["name"]}' → '{revised["name"]}'");
                Console.WriteLine($"    old input:    {old["input"]}  →  new input:    {revised["input"]}");
                Console.WriteLine($"    old expected: {old["expected"]}  →  new expected: {revised["expected"]}");
                currentTests[index] = revised;
            }

            store["test_cases"] = currentTests;
        }

        // ── Apply function-code revision ──────────────────────────────────
        if (revision.ContainsKey("function_code"))
        {
            var newCode = revision["function_code"]!.ToString()!;
            Console.WriteLine("Revising function code:");
            Console.WriteLine(newCode);
            store["function_code"] = newCode;
        }

        return "default";
    }
}

