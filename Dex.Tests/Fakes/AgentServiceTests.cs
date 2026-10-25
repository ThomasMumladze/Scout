using Dex.Application.Agent;
using Dex.Tests.Fakes;

namespace Dex.Tests.Agent;

public class AgentServiceTests
{
    private static AgentService CreateAgent(FakeLlmClient llm, params FakeTool[] tools)
        => new(llm, tools, new AgentOptions { MaxSteps = 3, MaxParseRetries = 2 });

    [Fact]
    public async Task Returns_final_answer_when_model_answers_directly()
    {
        var llm = new FakeLlmClient("""{"final_answer": "hello"}""");
        var agent = CreateAgent(llm);

        var result = await agent.RunAsync("hi");

        Assert.Equal("hello", result);
    }

    [Fact]
    public async Task Runs_tool_and_feeds_observation_back_to_model()
    {
        var llm = new FakeLlmClient(
            """{"tool": "calc", "args": {}}""",
            """{"final_answer": "done"}""");
        var agent = CreateAgent(llm, new FakeTool("calc", _ => "42"));

        var result = await agent.RunAsync("what is 6*7?");

        Assert.Equal("done", result);
        Assert.Contains("Observation: 42", llm.Calls[1][^1].Content);
    }

    [Fact]
    public async Task Tells_model_about_unknown_tool_and_recovers()
    {
        var llm = new FakeLlmClient(
            """{"tool": "nope", "args": {}}""",
            """{"final_answer": "ok"}""");
        var agent = CreateAgent(llm, new FakeTool("calc", _ => "42"));

        var result = await agent.RunAsync("test");

        Assert.Equal("ok", result);
        Assert.Contains("Unknown tool 'nope'", llm.Calls[1][^1].Content);
    }

    [Fact]
    public async Task Gives_up_after_too_many_invalid_responses()
    {
        var llm = new FakeLlmClient("garbage", "more garbage", "still garbage");
        var agent = CreateAgent(llm);

        var result = await agent.RunAsync("test");

        Assert.Contains("couldn't produce a valid response", result);
    }

    [Fact]
    public async Task Reports_tool_exception_to_model_instead_of_crashing()
    {
        var llm = new FakeLlmClient(
            """{"tool": "broken", "args": {}}""",
            """{"final_answer": "recovered"}""");
        var agent = CreateAgent(llm,
            new FakeTool("broken", _ => throw new InvalidOperationException("boom")));

        var result = await agent.RunAsync("test");

        Assert.Equal("recovered", result);
        Assert.Contains("Tool error: boom", llm.Calls[1][^1].Content);
    }

    [Fact]
    public async Task Stops_at_step_limit()
    {
        var call = """{"tool": "calc", "args": {}}""";
        var llm = new FakeLlmClient(call, call, call);
        var agent = CreateAgent(llm, new FakeTool("calc", _ => "1"));

        var result = await agent.RunAsync("test");

        Assert.Contains("step limit", result);
    }
}