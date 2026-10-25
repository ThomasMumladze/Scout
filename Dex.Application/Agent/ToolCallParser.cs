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

        // No braces at all (or in the wrong order) means there is nothing to parse
        if (start < 0 || end <= start)
            return new(null, null, "No JSON object found.");

        try
        {
            // [start..(end + 1)] is a range: it cuts out the substring from { to }
            // 'using var' disposes the document when the method ends
            using var doc = JsonDocument.Parse(modelOutput[start..(end + 1)]);
            var root = doc.RootElement;

            // Case 1: the model is done and gives the answer to the user
            if (root.TryGetProperty("final_answer", out var answer))
                return new(null, answer.GetString() ?? "", null);

            // Case 2: the model wants to use a tool
            if (root.TryGetProperty("tool", out var name))
            {
                // Clone so the element stays valid after the JsonDocument is disposed
                // If the model forgot "args", use an empty object instead of failing
                var args = root.TryGetProperty("args", out var a)
                    ? a.Clone()
                    : JsonDocument.Parse("{}").RootElement.Clone();

                return new(new ToolCall(name.GetString() ?? "", args), null, null);
            }

            // Valid JSON, but it has neither of the keys we expect
            return new(null, null, "JSON must contain either \"tool\" or \"final_answer\".");
        }
        catch (JsonException ex)
        {
            // The text between the braces is not valid JSON (missing quote, trailing comma, ...)
            // The message is sent back to the model so it can fix its own output
            return new(null, null, $"Invalid JSON: {ex.Message}");
        }
    }
}