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
        sb.AppendLine("- Choose the tool whose description matches the request.");
        sb.AppendLine("- If no tool fits the request, answer directly with final_answer and say that you cannot do it.");
        sb.AppendLine("- Never claim that an action was done (task added, file found, ...) unless you have seen the tool result for it.");
        sb.AppendLine("- All the tools you can use are listed below. Never say that a tool is missing or that you cannot search; try a tool first.");
        sb.AppendLine("- To find where a FILE is located by its name, use find_files. To find where code (a class or method) is defined or used, use search_files.");
        sb.AppendLine("- Every reply is ONE JSON object with exactly one top-level key: either \"tool\" or \"final_answer\", never both.");
        sb.AppendLine("- Use only the parameters listed for the tool. Do not invent extra parameters.");

        foreach (var t in tools)
        {
            sb.AppendLine($"- {t.Name}: {t.Description} Parameters: {t.Parameters}");
            if (!string.IsNullOrEmpty(t.Example))
                sb.AppendLine($"  Example: {t.Example}");
        }

        return sb.ToString();
    }
}