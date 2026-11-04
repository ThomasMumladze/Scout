using System.Text.Json;
using System.Text.RegularExpressions;
using Dex.Domain.Entities;

namespace Dex.Application.Agent;

/// <summary>
/// Result of parsing the model output: a tool call, a final answer, or an error.
/// A tool call and a final answer can both be set when the model sends both keys in one reply.
/// </summary>
public record AgentDecision(ToolCall? ToolCall, string? FinalAnswer, string? Error);

/// <summary>
/// Turns the raw text produced by the model into an AgentDecision.
/// The model is expected to reply with JSON in one of two shapes:
///   {"tool": "name", "args": {...}}  or  {"final_answer": "text"}
/// </summary>
public static partial class ToolCallParser
{
    // Matches zeros in front of a number value, e.g. the "0" in  "id": 01234
    [GeneratedRegex(@"(?<=:\s*)0+(?=\d)")]
    private static partial Regex LeadingZeros();

    public static AgentDecision Parse(string modelOutput)
    {
        var json = ExtractFirstObject(modelOutput);
        if (json is null)
            return new(null, null, "No JSON object found.");

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            // Fallback: small models sometimes write numbers with leading zeros (01234), which is not valid JSON.
            // The repair runs only after a normal parse failed, so valid JSON is never modified.
            try
            {
                doc = JsonDocument.Parse(LeadingZeros().Replace(json, ""));
            }
            catch (JsonException ex)
            {
                return new(null, null, $"Invalid JSON: {ex.Message}");
            }
        }

        using (doc)
        {
            try
            {
                return Interpret(doc.RootElement);
            }
            // GetString() throws InvalidOperationException when the value is not a string (e.g. {"final_answer": 5})
            catch (InvalidOperationException ex)
            {
                return new(null, null, $"Invalid JSON: {ex.Message}");
            }
        }
    }

    private static AgentDecision Interpret(JsonElement root)
    {
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

    /// <summary>
    /// Returns the first complete {...} object in the text, ignoring any text before or after it.
    /// Braces inside string values are skipped, so a message like "use { and }" does not confuse it.
    /// </summary>
    private static string? ExtractFirstObject(string text)
    {
        var start = text.IndexOf('{');
        if (start < 0) return null;

        var depth = 0;
        var inString = false;
        var escaped = false;

        for (var i = start; i < text.Length; i++)
        {
            var c = text[i];

            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            if (c == '"') inString = true;
            else if (c == '{') depth++;
            else if (c == '}' && --depth == 0) return text[start..(i + 1)];
        }

        // The object was never closed
        return null;
    }
}