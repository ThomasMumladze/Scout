using System.Text.Json;
using Dex.Domain.Entities;

namespace Dex.Infrastructure.Storage;

/// <summary>Keeps the to-do list in a JSON file.</summary>
public class TodoStore(string filePath)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task<List<TodoItem>> LoadAsync(CancellationToken ct = default)
    {
        // No file yet simply means an empty list
        if (!File.Exists(filePath)) return [];

        try
        {
            var json = await File.ReadAllTextAsync(filePath, ct);
            return JsonSerializer.Deserialize<List<TodoItem>>(json) ?? [];
        }
        catch (JsonException)
        {
            throw new InvalidOperationException($"The to-do file '{filePath}' is corrupted.");
        }
    }

    public async Task SaveAsync(List<TodoItem> items, CancellationToken ct = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(items, JsonOptions), ct);
    }
}