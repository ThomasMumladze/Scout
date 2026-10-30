using Dex.Application.Abstractions;
using System.Text.Json;
namespace Dex.Infrastructure.Tools;

/// <summary>
/// Name: search_files.
/// Description: searches a local project folder for lines containing the given text.
/// Parameters: query (string) - the text to look for.
/// Returns: up to 10 matches as "relative/path:line: text", or a "No matches" message.
/// </summary>
/// 
public class FileSearchTool(string rootDirectory) :ITool
{
    private static readonly string[] Extensions = [".cs", ".ts", ".tsx", ".js", ".jsx", ".json", ".scss", ".css", ".html", ".md", ".csproj"];
    private static readonly string[] IgnoredDirs = ["bin", "obj", "node_modules", ".git", ".vs"];
    private const int MaxResults = 10;

    private static readonly EnumerationOptions EnumerationSettings = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.Hidden | FileAttributes.System
    };

    public string Name => "search_files";

    public string Description => "Searches the user's local project files for lines containing a text. " +
        "Use it when the user asks where something is defined or used in the code.";

    public string Parameters => "{\"query\": \"string\"}";

    /// <summary>
    /// Runs the search_files tool.
    /// Expects args like {"query": "text"} and returns up to MaxResults matching lines,
    /// or a "No matches" message. Throws on invalid input; the agent loop reports
    /// the exception message back to the model as a tool error.
    /// </summary>
    public Task<string> RunAsync(JsonElement args, CancellationToken ct = default)
    {
        // Validate the input: the model must provide a non-empty "query" string
        if (!args.TryGetProperty("query", out var query) || string.IsNullOrWhiteSpace(query.GetString()))
            throw new ArgumentException("Missing or empty 'query' parameter.");

        // Validate the environment: the project folder must exist on disk
        if (!Directory.Exists(rootDirectory))
            throw new DirectoryNotFoundException($"The specified root directory '{rootDirectory}' does not exist.");

        // Extract the plain string from the JSON value ('!' is safe, it was checked above)
        var queryString = query.GetString()!;

        // Search is lazy, so Take(MaxResults) stops the scan after enough matches are found;
        // ToList() runs the search right now and stores the results
        var results = Search(queryString, ct).Take(MaxResults).ToList();

        // Return a readable message for the model: either "no matches" or one match per line.
        // The method is not async, so the result is wrapped in a completed Task
        return Task.FromResult(results.Count == 0
            ? $"No matches found for '{query}'."
            : string.Join("\n", results));

    }

    /// <summary>
    /// Lazily searches every supported file under the root directory and yields one result
    /// per matching line, formatted as "relative/path:lineNumber: line text".
    /// </summary>

    private IEnumerable<string> Search(string query, CancellationToken ct)
    {
        // Walk through all allowed files (bin, obj, node_modules, etc. are already filtered out)
        foreach (var file in EnumerateFiles(rootDirectory))
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


    /// <summary>
    /// Collects all files with supported extensions under <paramref name="rootDir"/>.
    /// Uses an explicit stack instead of recursion, so deep folder trees cannot overflow the call stack.
    /// </summary>
    private IEnumerable<string> EnumerateFiles(string rootDir)
    {
        var pending = new Stack<string>();
        pending.Push(rootDir);

        while (pending.Count > 0)
        {
            var dir = pending.Pop();

            // Files directly inside the current folder
            foreach (var file in Directory.EnumerateFiles(dir, "*", EnumerationSettings))
            {
                if (Extensions.Contains(Path.GetExtension(file).ToLowerInvariant()))
                    yield return file;
            }

            // Remember subfolders to visit later (unless they are on the ignore list)
            foreach (var sub in Directory.EnumerateDirectories(dir, "*", EnumerationSettings))
            {
                if (!IgnoredDirs.Contains(Path.GetFileName(sub)))
                    pending.Push(sub);
            }
        }
    }
}

