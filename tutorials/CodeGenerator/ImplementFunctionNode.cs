using System.Text;
using PocketFlow;

namespace CodeGenerator;

// Mirrors nodes.py::ImplementFunction
// Prompts the LLM to write a C# "public static object RunCode(...)" method
// and stores the code string in shared["function_code"].
public class ImplementFunctionNode : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        return ((string)store["problem"], (List<object>)store["test_cases"]);
    }

    protected override object? Execute(object? prepRes)
    {
        var (problem, testCases) = ((string, List<object>))prepRes!;
        Console.WriteLine("⚙️  Implementing C# solution...");

        var sb = new StringBuilder();
        for (var i = 0; i < testCases.Count; i++)
        {
            var tc = (Dictionary<object, object>)testCases[i];
            sb.AppendLine($"{i + 1}. {tc["name"]}");
            sb.AppendLine($"   input:    {tc["input"]}");
            sb.AppendLine($"   expected: {tc["expected"]}");
        }

        var prompt = $$"""
                       Implement a C# solution for this problem:

                       {{problem}}

                       Test cases to pass:
                       {{sb}}
                       IMPORTANT:
                       - The method MUST be named exactly "RunCode".
                       - It MUST be declared as: public static object RunCode(...)
                       - Parameter names MUST exactly match the keys shown in the test case inputs above.
                       - Do NOT include using statements, a namespace, or a class declaration — provide only the method.
                       - The return type in the signature must be object; cast the result explicitly if needed.

                       Output in this YAML format:
                       ```yaml
                       reasoning: |
                           To solve this I will...
                           My algorithm is...
                       function_code: |
                           public static object RunCode(int[] nums, int target)
                           {
                               // implementation
                               return result;
                           }
                       ```
                       """;

        var response = OllamaConnector.CallLlm(prompt);
        var result   = YamlHelper.ParseBlock(response);

        if (!result.ContainsKey("function_code"))
            throw new InvalidOperationException("LLM response is missing 'function_code'");

        var code = result["function_code"]?.ToString()
                   ?? throw new InvalidOperationException("'function_code' is null");

        if (!code.Contains("RunCode"))
            throw new InvalidOperationException(
                "Generated code does not contain 'RunCode' — the method must be named exactly RunCode");

        return code;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        var code  = (string)execRes!;

        store["function_code"] = code;

        Console.WriteLine("\n=== Implemented Function ===");
        Console.WriteLine(code);

        return "default";
    }
}