using System.Text.Json;
using Dex.Application.Abstractions;

namespace Dex.Infrastructure.Tools;

/// <summary>
/// Name: search_files.
/// Description: searches the text INSIDE the files of a local project folder for lines containing the given text.
/// Parameters: query (string) - the text to look for.
/// Returns: up to 10 matches as "relative/path:line: text", or a "No matches" message.
/// </summary>
public class FileSearchTool(string rootDirectory) : ITool
{
    // Only text-based files are searched; binary files make no sense for a line search
    private static readonly string[] Extensions =
        [".cs", ".ts", ".tsx", ".js", ".jsx", ".json", ".scss", ".css", ".html", ".md", ".csproj"];

    private const int MaxResults = 10;

    public string Name => "search_files";

    public string Description =>
        "Searches the text INSIDE the user's local C# project files for lines containing a text. " +
        "Use it when the user asks where something is defined or used in the code. " +
        "The query must be a short exact identifier such as a class or method name (for example 'ToolCallParser'). " +
        "Do not add language keywords like 'def' or 'class' to the query. " +
        "Do not use it to find a file by its name.";

    public string Parameters => "{\"query\": \"string\"}";

    public string Example => "{\"tool\": \"search_files\", \"args\": {\"query\": \"AgentService\"}}";

    /// <summary>
    /// Runs the search_files tool.
    /// Expects args like {"query": "text"} and returns up to MaxResults matching lines,
    /// or a "No matches" message. Throws on invalid input; the agent loop reports
    /// the exception message back to the model as a tool error.
    /// </summary>
    public Task<string> RunAsync(JsonElement args, CancellationToken ct = default)
    {
        // Validate the input: the model must provide a non-empty "query" string
        var queryString = ToolArgs.GetString(args, "query")
            ?? throw new ArgumentException("Missing or empty 'query' parameter.");

        // Validate the environment: the project folder must exist on disk
        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException($"The specified root directory '{rootDirectory}' does not exist.");

        // Search is lazy, so Take(MaxResults) stops the scan after enough matches are found;
        // ToList() runs the search right now and stores the results
        var results = Search(queryString, ct).Take(MaxResults).ToList();

        // Return a readable message for the model: either "no matches" or one match per line
        return Task.FromResult(results.Count == 0
            ? $"No matches found for '{queryString}'. Try again with only the bare identifier name, without extra words or keywords."
            : string.Join("\n", results));
    }

    /// <summary>
    /// Lazily searches every supported file under the root directory and yields one result
    /// per matching line, formatted as "relative/path:lineNumber: line text".
    /// </summary>
    private IEnumerable<string> Search(string query, CancellationToken ct)
    {
        // ProjectFiles skips bin, obj, node_modules etc.; we keep only text-based files
        var files = ProjectFiles.Enumerate(rootDirectory)
            .Where(f => Extensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        foreach (var file in files)
        {
            var lineNumber = 0;

            // ReadLines streams the file line by line instead of loading it all into memory
            foreach (var line in File.ReadLines(file))
            {
                // Allow the search to be cancelled between lines
                ct.ThrowIfCancellationRequested();
                lineNumber++;

                // Skip lines that do not contain the query (case-insensitive comparison)
                if (!line.Contains(query, StringComparison.OrdinalIgnoreCase)) continue;

                // Remove indentation and cut very long lines so the result stays short for the model
                var text = line.Trim();
                if (text.Length > 200) text = text[..200];

                // Return one match at a time (lazy), using a path relative to the project root
                yield return $"{Path.GetRelativePath(rootDirectory, file)}:{lineNumber}: {text}";
            }
        }
    }
}