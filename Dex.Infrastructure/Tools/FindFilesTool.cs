using System.Text.Json;
using Dex.Application.Abstractions;

namespace Dex.Infrastructure.Tools;

/// <summary>
/// Name: find_files.
/// Description: finds files by file name anywhere in the project folder, for any file type.
/// Parameters: query (string) - the file name or a part of it, e.g. "ITool.cs" or "AgentService".
/// Returns: up to 20 matching paths relative to the project folder, one per line, or a "No files found" message.
/// </summary>
public class FindFilesTool(string rootDirectory) : ITool
{
    private const int MaxResults = 20;

    public string Name => "find_files";

    public string Description =>
        "Finds files by their NAME anywhere in the user's project, for any file type. " +
        "Use it when the user asks where a file is located or whether a file exists. " +
        "The query is the file name or a part of it. Do not use it to search inside files.";

    public string Parameters => "{\"query\": \"string\"}";

    public string Example => "{\"tool\": \"find_files\", \"args\": {\"query\": \"AgentServiceTests.cs\"}}";

    public Task<string> RunAsync(JsonElement args, CancellationToken ct = default)
    {
        var query = ToolArgs.GetString(args, "query")?.Trim()
            ?? throw new ArgumentException("Missing or empty 'query' parameter.");

        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException($"Project folder not found: {rootDirectory}");

        var results = ProjectFiles.Enumerate(rootDirectory)
            .Where(file =>
            {
                ct.ThrowIfCancellationRequested();
                return Path.GetFileName(file).Contains(query, StringComparison.OrdinalIgnoreCase);
            })
            .Select(file => Path.GetRelativePath(rootDirectory, file))
            .Take(MaxResults)
            .ToList();

        return Task.FromResult(results.Count == 0
            ? $"No files found with '{query}' in the name. Try a shorter part of the name."
            : string.Join("\n", results));
    }
}