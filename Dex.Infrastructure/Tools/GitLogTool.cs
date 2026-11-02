using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using Dex.Application.Abstractions;

namespace Dex.Infrastructure.Tools;

/// <summary>
/// Name: git_log.
/// Description: shows the most recent commits of the git repository in the project folder.
/// Parameters: count (number, optional) - how many commits to show, 1-20, default 5.
/// Returns: one line per commit as "hash date message", or "No commits found."
/// </summary>
public class GitLogTool(string repoDirectory) : ITool
{
    private const int DefaultCount = 5;
    private const int MaxCount = 20;
    private static readonly TimeSpan GitTimeout = TimeSpan.FromSeconds(10);

    public string Name => "git_log";

    public string Description =>
        "Shows the most recent git commits of the user's project (hash, date, message). " +
        "Use it when the user asks what changed recently or what was committed. " +
        "Do not use it to search code.";

    public string Parameters => "{\"count\": \"number, optional (1-20, default 5)\"}";

    public async Task<string> RunAsync(JsonElement args, CancellationToken ct = default)
    {
        var count = DefaultCount;
        if (args.ValueKind == JsonValueKind.Object && args.TryGetProperty("count", out _))
        {
            if (!ToolArgs.TryGetInt(args, "count", out count) || count < 1 || count > MaxCount)
                throw new ArgumentException($"'count' must be a whole number between 1 and {MaxCount}.");
        }

        if (!Directory.Exists(repoDirectory))
            throw new DirectoryNotFoundException($"Project folder not found: {repoDirectory}");

        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = repoDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // ArgumentList (instead of one command string) means model-provided values can never inject extra commands
        foreach (var part in new[] { "log", "-n", count.ToString(), "--date=short", "--pretty=format:%h %ad %s" })
            startInfo.ArgumentList.Add(part);

        Process process;
        try
        {
            process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start git.");
        }
        catch (Win32Exception)
        {
            throw new InvalidOperationException("git is not installed or is not on PATH.");
        }

        using (process)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            // Give git a time limit so a stuck process cannot freeze the agent
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(GitTimeout);
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("git did not finish in time.");
            }

            var output = await outputTask;
            var error = await errorTask;

            // A non-zero exit code means git failed, e.g. "fatal: not a git repository"
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"git failed: {error.Trim()}");

            return string.IsNullOrWhiteSpace(output) ? "No commits found." : output.Trim();
        }
    }
}