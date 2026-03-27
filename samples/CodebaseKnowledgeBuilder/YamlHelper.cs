using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace CodebaseKnowledgeBuilder;

internal static class YamlHelper
{
    private static readonly IDeserializer Deserializer =
        new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

    /// <summary>Extracts the first ```yaml … ``` block from a response and parses it.</summary>
    public static T ParseYamlBlock<T>(string response)
    {
        var match = Regex.Match(response, @"```yaml(.*?)```",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var yaml = match.Success ? match.Groups[1].Value.Trim() : response.Trim();
        return Deserializer.Deserialize<T>(yaml);
    }
}