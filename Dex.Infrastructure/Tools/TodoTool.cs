using System.Text.Json;
using Dex.Application.Abstractions;
using Dex.Domain.Entities;
using Dex.Infrastructure.Storage;

namespace Dex.Infrastructure.Tools;

/// <summary>
/// Name: todo.
/// Description: manages the user's personal to-do list.
/// Parameters: action ("add", "list" or "done"), text (string, for "add"), id (number, for "done").
/// Returns: a confirmation message or the list of tasks, one per line as "[ ] 1: text" / "[x] 2: text".
/// </summary>
public class TodoTool(TodoStore store) : ITool
{
    public string Name => "todo";

    public string Description =>
        "Manages the user's personal to-do list: add a task, list the tasks, or mark a task as done. " +
        "Use it when the user wants to remember, add, see or finish a task. Do not use it to search code.";

    public string Parameters =>
        "{\"action\": \"add | list | done\", \"text\": \"string (only for add)\", \"id\": \"number (only for done)\"}";

    public async Task<string> RunAsync(JsonElement args, CancellationToken ct = default)
    {
        var action = ToolArgs.GetString(args, "action")
            ?? throw new ArgumentException("Missing parameter 'action'. Use add, list or done.");

        var items = await store.LoadAsync(ct);

        switch (action.Trim().ToLowerInvariant())
        {
            case "add":
                {
                    var text = ToolArgs.GetString(args, "text")
                        ?? throw new ArgumentException("Action 'add' needs a non-empty 'text'.");
                    var id = items.Count == 0 ? 1 : items.Max(i => i.Id) + 1;
                    items.Add(new TodoItem(id, text.Trim(), false));
                    await store.SaveAsync(items, ct);
                    return $"Added task {id}: {text.Trim()}";
                }

            case "list":
                return items.Count == 0
                    ? "The to-do list is empty."
                    : string.Join("\n", items.Select(i => $"[{(i.Done ? "x" : " ")}] {i.Id}: {i.Text}"));

            case "done":
                {
                    if (!ToolArgs.TryGetInt(args, "id", out var id))
                        throw new ArgumentException("Action 'done' needs a numeric 'id'.");
                    var index = items.FindIndex(i => i.Id == id);
                    if (index < 0)
                        throw new ArgumentException($"No task with id {id}.");
                    items[index] = items[index] with { Done = true };
                    await store.SaveAsync(items, ct);
                    return $"Marked task {id} as done.";
                }

            default:
                throw new ArgumentException($"Unknown action '{action}'. Use add, list or done.");
        }
    }
}