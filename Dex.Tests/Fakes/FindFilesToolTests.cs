using System.Text.Json;
using Dex.Infrastructure.Tools;

namespace Dex.Tests.Tools;

public class FindFilesToolTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), $"find-{Guid.NewGuid()}");

    public FindFilesToolTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "src", "deep"));
        Directory.CreateDirectory(Path.Combine(_root, "bin"));
        File.WriteAllText(Path.Combine(_root, "src", "deep", "AgentServiceTests.cs"), "x");
        File.WriteAllText(Path.Combine(_root, "src", "notes.txt"), "x");
        File.WriteAllText(Path.Combine(_root, "bin", "AgentServiceTests.dll"), "x");
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    private static JsonElement Args(string json) => JsonDocument.Parse(json).RootElement.Clone();

    [Fact]
    public async Task Finds_file_in_subfolder_by_part_of_name_ignoring_case()
    {
        var tool = new FindFilesTool(_root);

        var result = await tool.RunAsync(Args("""{"query": "agentservicetests"}"""));

        Assert.Equal(Path.Combine("src", "deep", "AgentServiceTests.cs"), result);
    }

    [Fact]
    public async Task Finds_files_of_any_type()
    {
        var tool = new FindFilesTool(_root);

        var result = await tool.RunAsync(Args("""{"query": "notes.txt"}"""));

        Assert.Equal(Path.Combine("src", "notes.txt"), result);
    }

    [Fact]
    public async Task Reports_when_nothing_matches()
    {
        var tool = new FindFilesTool(_root);

        var result = await tool.RunAsync(Args("""{"query": "missing"}"""));

        Assert.StartsWith("No files found", result);
    }

    [Fact]
    public async Task Throws_when_query_is_missing()
    {
        var tool = new FindFilesTool(_root);

        await Assert.ThrowsAsync<ArgumentException>(() => tool.RunAsync(Args("{}")));
    }
}