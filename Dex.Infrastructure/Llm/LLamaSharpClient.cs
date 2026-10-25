using System.Text;
using Dex.Application.Abstractions;
using Dex.Domain.Entities;
using LLama;
using LLama.Common;
using LLama.Sampling;

namespace Dex.Infrastructure.Llm;

/// <summary>
/// ILlmClient implementation that runs a local GGUF model through LLamaSharp (llama.cpp).
/// The model is loaded once in the constructor and reused for every request.
/// </summary>
public sealed class LLamaSharpClient : ILlmClient, IDisposable
{
    private readonly LlmOptions _options;
    private readonly ModelParams _modelParams;
    private readonly LLamaWeights _weights;

    public LLamaSharpClient(LlmOptions options)
    {
        _options = options;

        // Model settings: file path, how many tokens fit in the context window,
        // and how many layers run on the GPU (0 = CPU only)
        _modelParams = new ModelParams(options.ModelPath)
        {
            ContextSize = options.ContextSize,
            GpuLayerCount = options.GpuLayerCount
        };

        // Load the model weights into memory (slow, so it is done only once)
        _weights = LLamaWeights.LoadFromFile(_modelParams);
    }

    /// <summary>
    /// Sends the whole conversation to the model and returns its reply as text.
    /// </summary>
    public async Task<string> CompleteAsync(IReadOnlyList<ChatMessage> messages, CancellationToken ct = default)
    {
        // A stateless executor keeps no memory between calls; we resend the full
        // conversation each time, so every request starts from a clean context
        var executor = new StatelessExecutor(_weights, _modelParams);

        var inferenceParams = new InferenceParams
        {
            // Hard limit on the length of the reply
            MaxTokens = _options.MaxTokens,

            // Stop generating as soon as the model writes the end-of-message marker
            AntiPrompts = ["<|im_end|>"],

            // Low temperature = more predictable output, which keeps the JSON format stable
            SamplingPipeline = new DefaultSamplingPipeline { Temperature = _options.Temperature }
        };

        // The model produces its answer token by token (small pieces of text);
        // we collect them into one string
        var sb = new StringBuilder();
        await foreach (var token in executor.InferAsync(BuildPrompt(messages), inferenceParams, ct))
            sb.Append(token);

        // Remove the stop marker if it leaked into the output, and trim whitespace
        return sb.ToString().Replace("<|im_end|>", "").Trim();
    }

    // Qwen uses the ChatML format:
    // <|im_start|>role
    // message text<|im_end|>
    private static string BuildPrompt(IReadOnlyList<ChatMessage> messages)
    {
        var sb = new StringBuilder();
        foreach (var m in messages)
        {
            // Map our roles to the ones the model knows (system, user, assistant)
            var role = m.Role switch
            {
                Role.System => "system",
                Role.Assistant => "assistant",
                _ => "user"          // User and Tool observations
            };
            sb.Append($"<|im_start|>{role}\n{m.Content}<|im_end|>\n");
        }

        // Open an assistant turn at the end so the model continues with its reply
        sb.Append("<|im_start|>assistant\n");
        return sb.ToString();
    }

    /// <summary>Frees the model memory (can be several GB) when the client is no longer needed.</summary>
    public void Dispose() => _weights.Dispose();
}