using System.Text;
using Dex.Application.Abstractions;

namespace Dex.Application.Prompts;

public static class SystemPromptBuilder
{
    /// <summary>Builds the system prompt that explains the JSON protocol and lists the available tools.</summary>
    public static string Build(IEnumerable<ITool> tools)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an assistant that can use tools. Respond with JSON only, no other text.");
        sb.AppendLine("To use a tool: {\"tool\": \"<name>\", \"args\": {...}}");
        sb.AppendLine("To answer the user: {\"final_answer\": \"<your answer>\"}");
        sb.AppendLine();
        sb.AppendLine("Available tools:");
        foreach (var t in tools)
            sb.AppendLine($"- {t.Name}: {t.Description} Parameters: {t.Parameters}");
        return sb.ToString();
    }
}