using Dex.Application.Abstractions;
using Dex.Application.Prompts;
using Dex.Domain.Entities;

namespace Dex.Application.Agent;

/// <summary>
/// The agent loop: sends the conversation to the model, runs the tools the model asks for,
/// feeds the results back, and repeats until the model gives a final answer.
/// </summary>
public class AgentService(ILlmClient llm, IEnumerable<ITool> tools, AgentOptions options)
{
    // Look up tools by name, because the model refers to them by name in its JSON
    private readonly Dictionary<string, ITool> _tools = tools.ToDictionary(t => t.Name);

    /// <summary>Answers a user question, letting the model call tools until it gives a final answer.</summary>
    public async Task<string> RunAsync(string userInput, CancellationToken ct = default)
    {
        // The conversation starts with the system prompt (rules + tool list) and the user's question.
        // This list grows with every step, so the model always sees the full history
        var messages = new List<ChatMessage>
        {
            new(Role.System, SystemPromptBuilder.Build(_tools.Values)),
            new(Role.User, userInput)
        };

        var parseFailures = 0;

        // Each iteration is one model call; MaxSteps prevents an endless loop
        for (var step = 0; step < options.MaxSteps; step++)
        {
            // Ask the model what to do next and remember its reply in the history
            var reply = await llm.CompleteAsync(messages, ct);
            messages.Add(new(Role.Assistant, reply));

            // Interpret the reply: final answer, tool call, or error
            var decision = ToolCallParser.Parse(reply);

            // The model is done: return its answer to the user
            if (decision.FinalAnswer is not null)
                return decision.FinalAnswer;

            // Invalid JSON: ask the model to try again, but only a limited number of times
            if (decision.Error is not null)
            {
                if (++parseFailures > options.MaxParseRetries)
                    return "Sorry, I couldn't produce a valid response.";
                messages.Add(new(Role.User, $"Your response was invalid: {decision.Error} Reply with valid JSON only."));
                continue; // skip to the next step without running any tool
            }

            // The model picked a tool that does not exist: tell it which ones do
            var call = decision.ToolCall!;
            if (!_tools.TryGetValue(call.name, out var tool))
            {
                messages.Add(new(Role.User,
                    $"Unknown tool '{call.name}'. Available tools: {string.Join(", ", _tools.Keys)}."));
                continue;
            }

            // A failing tool must not crash the agent; the error goes back to the model instead
            string observation;
            try
            {
                observation = await tool.RunAsync(call.Args, ct);
            }
            catch (Exception ex)
            {
                observation = $"Tool error: {ex.Message}";
            }

            // Give the tool result to the model; the next iteration lets it react to it
            messages.Add(new(Role.Tool, $"Observation: {observation}"));
        }

        // The loop ended without a final answer
        return "Sorry, I reached the step limit without an answer.";
    }
}