using System.Text.Json;
using Dex.Application.Abstractions;

namespace Dex.Tests.Fakes;

public class FakeTool(string name, Func<JsonElement, string> run) : ITool
{
    public string Name => name;
    public string Description => "Fake tool for tests.";
    public string Parameters => "{}";

    public Task<string> RunAsync(JsonElement args, CancellationToken ct = default)
        => Task.FromResult(run(args));
}