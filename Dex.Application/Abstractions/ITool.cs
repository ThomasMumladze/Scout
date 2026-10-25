using System.Text.Json;

namespace Dex.Application.Abstractions
{/// <summary>A tool the agent can use.</summary>
    public interface ITool
    {
        /// <summary>Unique name the model uses to pick the tool.</summary>
        string Name { get; }

        /// <summary>Description for the model: what the tool does and when to use it.</summary>
        string Description { get; }

        /// <summary>Parameter description, e.g. {"query": "string"}.</summary>
        string Parameters { get; }

        /// <summary>R+uns the tool and returns the result as text.</summary>
        Task<string> RunAsync(JsonElement args, CancellationToken ct = default);
    }
}
