using Dex.Domain.Entities;

namespace Dex.Application.Abstractions
{
    /// <summary>
    /// Abstraction over a language model. The agent depends only on this interface,
    /// so the real model (LLamaSharp) can be replaced by a fake one in tests
    /// or by another provider without touching the agent logic.
    /// </summary>
    public interface ILlmClient
    {
        /// <summary>
        /// Sends the whole conversation to the model and returns its reply.
        /// </summary>
        /// <param name="messages">The conversation so far: system prompt, user messages, model replies and tool results.</param>
        /// <param name="ct">Token that allows the request to be cancelled.</param>
        /// <returns>The raw text produced by the model.</returns>
        Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct = default);
    }
}