using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodeGenerator;

internal static class YamlHelper
{
    private static readonly IDeserializer Deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

    public static Dictionary<object, object> ParseBlock(string llmResponse)
    {
        var block = ExtractBlock(llmResponse);
        return ParseSafely(block);
    }

    private static string ExtractBlock(string text)
    {
        var match = Regex.Match(text, @"```yaml(.*?)```",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups[1].Value.Trim() : text.Trim();
    }

    private static Dictionary<object, object> ParseSafely(string block)
    {
        // First pass — try as-is
        try
        {
            return Deserializer.Deserialize<Dictionary<object, object>>(block)
                   ?? throw new InvalidOperationException("YAML deserialized to null.");
        }
        catch (YamlException)
        {
            // Second pass — rewrite bare scalar lines for known block-scalar keys
            var blockKeys = new HashSet<string> { "reasoning", "function_code" };
            var fixedLines = block.Split('\n').Select(line =>
            {
                var m = Regex.Match(line, @"^(\w+):\s*(.*)$");
                if (m.Success && blockKeys.Contains(m.Groups[1].Value) && !line.Contains('|'))
                {
                    var key = m.Groups[1].Value;
                    var val = m.Groups[2].Value.Trim();
                    return string.IsNullOrEmpty(val) ? $"{key}: |" : $"{key}: |\n  {val}";
                }
                return line;
            });

            var fixedBlock = string.Join("\n", fixedLines);
            try
            {
                return Deserializer.Deserialize<Dictionary<object, object>>(fixedBlock)
                       ?? throw new InvalidOperationException("YAML deserialized to null.");
            }
            catch (YamlException ex)
            {
                throw new InvalidOperationException(
                    $"Unable to parse LLM YAML response:\n{block}", ex);
            }
        }
    }
}