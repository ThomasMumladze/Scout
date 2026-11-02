using System.Text.Json;

namespace Dex.Infrastructure.Tools;

/// <summary>Helpers for reading arguments the model produced; small models are sloppy about types.</summary>
internal static class ToolArgs
{
    /// <summary>Reads a string argument. Returns null if it is missing, not a string, or blank.</summary>
    public static string? GetString(JsonElement args, string name)
    {
        if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(name, out var value))
            return null;

        var text = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    /// <summary>Reads an integer argument given either as a number (5) or as a string ("5").</summary>
    public static bool TryGetInt(JsonElement args, string name, out int result)
    {
        result = 0;
        if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty(name, out var value))
            return false;

        return value.ValueKind switch
        {
            JsonValueKind.Number => value.TryGetInt32(out result),
            JsonValueKind.String => int.TryParse(value.GetString(), out result),
            _ => false
        };
    }
}