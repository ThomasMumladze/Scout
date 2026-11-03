using System.Text.Json;
using Dex.Domain.Entities;

namespace Dex.Application.Agent;

/// <summary>
/// Result of parsing the model output. Exactly one of the three fields is set:
/// a tool call to run, a final answer for the user, or an error message.
/// </summary>
public record AgentDecision(ToolCall? ToolCall, string? FinalAnswer, string? Error);

/// <summary>
/// Turns the raw text produced by the model into an AgentDecision.
/// The model is expected to reply with JSON in one of two shapes:
///   {"tool": "name", "args": {...}}  or  {"final_answer": "text"}
/// </summary>
public static class ToolCallParser
{
    public static AgentDecision Parse(string modelOutput)
    {
        // Small models often wrap JSON in extra text, so take everything between the first { and the last }
        var start = modelOutput.IndexOf('{');
        var end = modelOutput.LastIndexOf('}');
        if (start < 0 || end <= start)
            return new(null, null, "No JSON object found.");

        try
        {
            using var doc = JsonDocument.Parse(modelOutput[start..(end + 1)]);
            var root = doc.RootElement;

            var hasAnswer = root.TryGetProperty("final_answer", out var answer);
            var hasTool = root.TryGetProperty("tool", out var name);

            if (hasTool)
            {
                // Clone so the element stays valid after the JsonDocument is disposed
                // If the model forgot "args", use an empty object instead of failing
                var args = root.TryGetProperty("args", out var a)
                    ? a.Clone()
                    : JsonDocument.Parse("{}").RootElement.Clone();

                // When the model sends both keys, keep both: the agent decides which one to trust
                var answerText = hasAnswer ? answer.GetString() ?? "" : null;
                return new(new ToolCall(name.GetString() ?? "", args), answerText, null);
            }

            if (hasAnswer)
                return new(null, answer.GetString() ?? "", null);

            return new(null, null, "JSON must contain either \"tool\" or \"final_answer\".");
        }
        // GetString() throws InvalidOperationException when the value is not a string (e.g. {"final_answer": 5})
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            return new(null, null, $"Invalid JSON: {ex.Message}");
        }
    }
}