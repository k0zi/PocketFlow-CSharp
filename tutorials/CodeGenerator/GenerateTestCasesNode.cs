using PocketFlow;

namespace CodeGenerator;

// Mirrors nodes.py::GenerateTestCases
// Prompts the LLM for 5-7 C# test cases and stores them in shared["test_cases"].
public class GenerateTestCasesNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return (string)store["problem"];
    }

    protected override object? Execute(object? prepRes)
    {
        var problem = (string)prepRes!;
        Console.WriteLine("🧪 Generating test cases...");

        var prompt = $$"""
                       Generate 5-7 test cases for this C# coding problem:

                       {{problem}}

                       IMPORTANT:
                       - Parameter names in 'input' must exactly match the C# parameter names that RunCode will use.
                       - Use simple scalar or list values only (no nested objects).

                       Output in this YAML format:
                       ```yaml
                       reasoning: |
                           The parameters should be...
                           I will consider basic, edge and corner cases.
                       test_cases:
                         - name: "Basic case"
                           input: {param1: value1, param2: value2}
                           expected: result1
                         - name: "Edge case - empty"
                           input: {param1: value3, param2: value4}
                           expected: result2
                       ```
                       """;

        var response  = OllamaConnector.CallLlm(prompt);
        var result    = YamlHelper.ParseBlock(response);

        if (!result.ContainsKey("test_cases"))
            throw new InvalidOperationException("LLM response is missing 'test_cases'");

        var testCases = (List<object>)result["test_cases"];
        for (var i = 0; i < testCases.Count; i++)
        {
            var tc = (Dictionary<object, object>)testCases[i];
            if (!tc.ContainsKey("name"))
                throw new InvalidOperationException($"Test case {i} is missing 'name'");
            if (!tc.ContainsKey("input"))
                throw new InvalidOperationException($"Test case {i} is missing 'input'");
            if (!tc.ContainsKey("expected"))
                throw new InvalidOperationException($"Test case {i} is missing 'expected'");
            if (tc["input"] is not Dictionary<object, object>)
                throw new InvalidOperationException($"Test case {i} 'input' must be a mapping");
        }

        return result;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store     = (Dictionary<string, object>)shared;
        var result    = (Dictionary<object, object>)execRes!;
        var testCases = (List<object>)result["test_cases"];

        store["test_cases"] = testCases;

        Console.WriteLine($"\n=== Generated {testCases.Count} Test Cases ===");
        for (var i = 0; i < testCases.Count; i++)
        {
            var tc = (Dictionary<object, object>)testCases[i];
            Console.WriteLine($"{i + 1}. {tc["name"]}");
            Console.WriteLine($"   input:    {tc["input"]}");
            Console.WriteLine($"   expected: {tc["expected"]}");
        }

        return "default";
    }
}