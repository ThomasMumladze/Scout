namespace Dex.Infrastructure.Llm
{
    public class LlmOptions
    {
        /// <summary>Full path to the .gguf model file.</summary>
        public required string ModelPath { get; set; }

        /// <summary>How many tokens fit in the model's context window.</summary>
        public uint ContextSize { get; set; } = 4096;

        /// <summary>Number of layers to run on the GPU (0 = CPU only).</summary>
        public int GpuLayerCount { get; set; } = 0;

        /// <summary>Maximum number of tokens in one reply.</summary>
        public int MaxTokens { get; set; } = 512;

        /// <summary>Lower values make the output more predictable.</summary>
        public float Temperature { get; set; } = 0.2f;
    }
}
