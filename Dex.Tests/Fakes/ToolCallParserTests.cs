using Dex.Application.Agent;

namespace Dex.Tests.Agent;

public class ToolCallParserTests
{
    [Fact]
    public void Repairs_leading_zero_number_and_ignores_trailing_junk()
    {
        // The exact output a real model produced: "id": 01234 and an extra "} at the end
        var decision = ToolCallParser.Parse(
            """{"final_answer": "added", "tool": "todo", "args": {"action": "add", "id": 01234}}"}""");

        Assert.Null(decision.Error);
        Assert.Equal("todo", decision.ToolCall!.name);
        Assert.Equal(1234, decision.ToolCall.Args.GetProperty("id").GetInt32());
    }

    [Fact]
    public void Ignores_braces_inside_string_values()
    {
        var decision = ToolCallParser.Parse("""Sure: {"final_answer": "use { and } carefully"} thanks""");

        Assert.Equal("use { and } carefully", decision.FinalAnswer);
    }

    [Fact]
    public void Keeps_both_tool_call_and_answer_when_reply_has_both_keys()
    {
        var decision = ToolCallParser.Parse("""{"final_answer": "x", "tool": "calc", "args": {}}""");

        Assert.NotNull(decision.ToolCall);
        Assert.Equal("x", decision.FinalAnswer);
    }

    [Fact]
    public void Reports_error_for_unclosed_json()
    {
        var decision = ToolCallParser.Parse("""{"tool": "calc", "args": {""");

        Assert.NotNull(decision.Error);
    }
}