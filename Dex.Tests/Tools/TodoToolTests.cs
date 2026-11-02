using System.Text.Json;
using Dex.Infrastructure.Storage;
using Dex.Infrastructure.Tools;

namespace Dex.Tests.Tools;

public class TodoToolTests : IDisposable
{
    // Every test gets its own temporary file, so tests never affect each other or the real list
    private readonly string _file = Path.Combine(Path.GetTempPath(), $"todos-{Guid.NewGuid()}.json");

    private TodoTool CreateTool() => new(new TodoStore(_file));

    private static JsonElement Args(string json) => JsonDocument.Parse(json).RootElement.Clone();

    public void Dispose()
    {
        if (File.Exists(_file)) File.Delete(_file);
    }

    [Fact]
    public async Task Added_task_appears_in_list()
    {
        var tool = CreateTool();

        await tool.RunAsync(Args("""{"action": "add", "text": "write README"}"""));
        var list = await tool.RunAsync(Args("""{"action": "list"}"""));

        Assert.Equal("[ ] 1: write README", list);
    }

    [Fact]
    public async Task Done_marks_task_and_accepts_id_as_string()
    {
        var tool = CreateTool();
        await tool.RunAsync(Args("""{"action": "add", "text": "task"}"""));

        await tool.RunAsync(Args("""{"action": "done", "id": "1"}"""));
        var list = await tool.RunAsync(Args("""{"action": "list"}"""));

        Assert.Equal("[x] 1: task", list);
    }

    [Fact]
    public async Task Done_with_unknown_id_throws()
    {
        var tool = CreateTool();

        await Assert.ThrowsAsync<ArgumentException>(
            () => tool.RunAsync(Args("""{"action": "done", "id": 99}""")));
    }

    [Fact]
    public async Task Unknown_action_throws()
    {
        var tool = CreateTool();

        await Assert.ThrowsAsync<ArgumentException>(
            () => tool.RunAsync(Args("""{"action": "delete"}""")));
    }
}