using System.Text.Json;

namespace Dex.Domain.Entities
{
    /// <summary>The tool chosen by the model and its arguments.</summary>
    public record ToolCall(string name, JsonElement Args);
}
