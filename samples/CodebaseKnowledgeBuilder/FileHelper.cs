namespace CodebaseKnowledgeBuilder;

internal static class FileHelper
{
    public static Dictionary<string, string> GetContentForIndices(
        List<(string path, string content)> files, IEnumerable<int> indices)
    {
        var map = new Dictionary<string, string>();
        foreach (var i in indices)
            if (i >= 0 && i < files.Count)
            {
                var (path, content) = files[i];
                map[$"{i} # {path}"] = content;
            }
        return map;
    }
}