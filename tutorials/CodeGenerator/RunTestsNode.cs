using System.Text.Json;
using PocketFlow;

namespace CodeGenerator;

// Mirrors nodes.py::RunTests (BatchNode)
// Compiles and runs the C# RunCode method against every test case in parallel
// via BatchNode, then decides "success" / "failure" / "max_iterations".
public class RunTestsNode : BatchNode
{
    // Prep returns a list; BatchNode calls Exec once per element.
    protected override object? Prepare(object shared)
    {
        var store        = (Dictionary<string, object>)shared;
        var functionCode = (string)store["function_code"];
        var testCases    = (List<object>)store["test_cases"];

        return testCases
            .Select(tc => (functionCode, (Dictionary<object, object>)tc))
            .ToList();
    }

    // Exec receives one (functionCode, testCase) tuple at a time.
    protected override object? Execute(object? prepRes)
    {
        var (functionCode, testCase) = ((string, Dictionary<object, object>))prepRes!;
        var input    = (Dictionary<object, object>)testCase["input"];
        var expected = testCase["expected"];

        var (actual, error) = Utils.ExecuteCode(functionCode, input);

        if (error is not null)
        {
            return new Dictionary<string, object?>
            {
                ["test_case"] = testCase,
                ["passed"]    = (object)false,
                ["actual"]    = null,
                ["expected"]  = expected,
                ["error"]     = error
            };
        }

        var passed = Utils.ValuesEqual(actual, expected);
        return new Dictionary<string, object?>
        {
            ["test_case"] = testCase,
            ["passed"]    = (object)passed,
            ["actual"]    = actual,
            ["expected"]  = expected,
            ["error"]     = passed
                ? null
                : (object?)$"Expected {Str(expected)}, got {Str(actual)}"
        };
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;

        var results = ((List<object?>)execRes!)
            .Select(r => (Dictionary<string, object?>)r!)
            .ToList();

        store["test_results"]    = results;
        store["iteration_count"] = (int)store.GetValueOrDefault("iteration_count", 0) + 1;

        var iteration = (int)store["iteration_count"];
        var passed    = results.Count(r => (bool)r["passed"]!);
        var total     = results.Count;
        var allPassed = passed == total;

        Console.WriteLine($"\n=== Test Results: {passed}/{total} Passed ===");

        var failed = results.Where(r => !(bool)r["passed"]!).ToList();
        if (failed.Count > 0)
        {
            Console.WriteLine("Failed tests:");
            for (var i = 0; i < failed.Count; i++)
            {
                var r  = failed[i];
                var tc = (Dictionary<object, object>)r["test_case"]!;
                Console.WriteLine($"{i + 1}. {tc["name"]}:");
                Console.WriteLine(r["error"] is not null
                    ? $"   error:    {r["error"]}"
                    : $"   actual:   {Str(r["actual"])}");
                Console.WriteLine($"   expected: {Str(r["expected"])}");
            }
        }

        if (allPassed) return "success";

        var maxIter = (int)store.GetValueOrDefault("max_iterations", 5);
        return iteration >= maxIter ? "max_iterations" : "failure";
    }

    private static string Str(object? v) =>
        v is null ? "null" : JsonSerializer.Serialize(v, v.GetType());
}