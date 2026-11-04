namespace Dex.Infrastructure.Tools;

/// <summary>Walks a project folder and returns every file, skipping build output and other noise.</summary>
internal static class ProjectFiles
{
    private static readonly string[] IgnoredDirs = ["bin", "obj", "node_modules", ".git", ".vs"];

    // Do not skip ReparsePoint here: OneDrive placeholders have that attribute too
    private static readonly EnumerationOptions Options = new()
    {
        IgnoreInaccessible = true,
        AttributesToSkip = FileAttributes.System
    };

    /// <summary>Uses an explicit stack instead of recursion, so deep folder trees cannot overflow the call stack.</summary>
    public static IEnumerable<string> Enumerate(string rootDir)
    {
        var pending = new Stack<string>();
        pending.Push(rootDir);

        while (pending.Count > 0)
        {
            var dir = pending.Pop();

            foreach (var file in Directory.EnumerateFiles(dir, "*", Options))
                yield return file;

            foreach (var sub in Directory.EnumerateDirectories(dir, "*", Options))
            {
                if (IgnoredDirs.Contains(Path.GetFileName(sub))) continue;

                // Real symlinks/junctions can point back to a parent folder and form endless loops
                if (new DirectoryInfo(sub).LinkTarget is not null) continue;

                pending.Push(sub);
            }
        }
    }
}