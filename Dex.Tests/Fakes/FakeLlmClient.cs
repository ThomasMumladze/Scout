using Dex.Application.Abstractions;
using Dex.Domain.Entities;

namespace Dex.Tests.Fakes;

/// <summary>Returns predefined responses in order and remembers the messages it received.</summary>
public class FakeLlmClient(params string[] responses) : ILlmClient
{
    private readonly Queue<string> _responses = new(responses);

    /// <summary>A copy of the messages received on each call.</summary>
    public List<List<ChatMessage>> Calls { get; } = [];

    public Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        Calls.Add(messages.ToList());

        if (_responses.Count == 0)
            throw new InvalidOperationException("FakeLlmClient ran out of responses.");

        return Task.FromResult(_responses.Dequeue());
    }
}