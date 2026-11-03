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

    [Fact]
    public async Task Runs_tool_and_ignores_final_answer_when_response_has_both()
    {
        var llm = new FakeLlmClient(
            """{"final_answer": "Task added", "tool": "calc", "args": {}}""",
            """{"final_answer": "done"}""");
        var agent = CreateAgent(llm, new FakeTool("calc", _ => "42"));

        var result = await agent.RunAsync("test");

        // The unverified answer is not returned; the tool ran and its result was fed back to the model
        Assert.Equal("done", result);
        Assert.Contains("Observation: 42", llm.Calls[1][^1].Content);
    }

    [Fact]
    public async Task Accepts_final_answer_sent_together_with_tool_once_a_tool_result_exists()
    {
        var both = """{"final_answer": "answer after tool", "tool": "calc", "args": {}}""";
        var llm = new FakeLlmClient(both, both);
        var agent = CreateAgent(llm, new FakeTool("calc", _ => "42"));

        var result = await agent.RunAsync("test");

        // First reply: tool runs, answer ignored. Second reply: tool result already seen, so the answer is accepted.
        Assert.Equal("answer after tool", result);
    }

    [Fact]
    public async Task Recovers_when_final_answer_is_not_a_string()
    {
        var llm = new FakeLlmClient(
            """{"final_answer": 5}""",
            """{"final_answer": "five"}""");
        var agent = CreateAgent(llm);

        var result = await agent.RunAsync("test");

        Assert.Equal("five", result);
    }
}