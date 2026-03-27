using CodebaseKnowledgeBuilder.Utils;
using PocketFlow;

namespace CodebaseKnowledgeBuilder;

public class FetchRepo : Node
{
    protected override object? Prepare(object shared)
    {
        var store = (Dictionary<string, object>)shared;
        var repoUrl    = SharedStore.Get<string?>(store, "repo_url", null);
        var localDir   = SharedStore.Get<string?>(store, "local_dir", null);
        var projectName = SharedStore.Get<string?>(store, "project_name", null);

        if (string.IsNullOrEmpty(projectName))
        {
            projectName = !string.IsNullOrEmpty(repoUrl)
                ? repoUrl.Split('/').Last().Replace(".git", "")
                : Path.GetFullPath(localDir!).Split(Path.DirectorySeparatorChar).Last();
            store["project_name"] = projectName;
        }

        return new Dictionary<string, object?>
        {
            ["repo_url"]          = repoUrl,
            ["local_dir"]         = localDir,
            ["token"]             = SharedStore.Get<string?>(store, "github_token", null),
            ["include_patterns"]  = SharedStore.Get(store, "include_patterns", new List<string>()),
            ["exclude_patterns"]  = SharedStore.Get(store, "exclude_patterns", new List<string>()),
            ["max_file_size"]     = SharedStore.Get<long>(store, "max_file_size", 100_000),
        };
    }

    protected override object? Execute(object? prepRes)
    {
        var p = (Dictionary<string, object?>)prepRes!;
        var repoUrl   = p["repo_url"] as string;
        var localDir  = p["local_dir"] as string;
        var token     = p["token"] as string;
        var include   = (List<string>)p["include_patterns"]!;
        var exclude   = (List<string>)p["exclude_patterns"]!;
        var maxSize   = (long)p["max_file_size"]!;

        Dictionary<string, string> raw;
        if (!string.IsNullOrEmpty(repoUrl))
        {
            Console.WriteLine($"Crawling repository: {repoUrl}...");
            raw = CrawlGithubFiles.Crawl(repoUrl, token, maxSize, true, include, exclude);
        }
        else
        {
            Console.WriteLine($"Crawling directory: {localDir}...");
            raw = CrawlLocalFiles.Crawl(localDir!, include, exclude, maxSize, true);
        }

        if (raw.Count == 0)
            throw new InvalidOperationException("Failed to fetch files – result was empty.");

        var files = raw.Select(kv => (kv.Key, kv.Value)).ToList();
        Console.WriteLine($"Fetched {files.Count} files.");
        return files;
    }

    protected override object? Post(object shared, object? prepRes, object? execRes)
    {
        var store = (Dictionary<string, object>)shared;
        store["files"] = execRes!;
        return null;
    }
}