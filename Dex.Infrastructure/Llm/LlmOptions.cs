namespace Dex.Infrastructure.Llm
{
    public class LlmOptions
    {
        public required string ModelPath { get; set; }
        public uint ContextSize { get; set; } = 4096;
        public int GpuLayerCount { get; set; } = 0; // cpu only
        public int MaxTokens { get; set; } = 512;
        public float Temperature { get; set; } = 0.2f;
    }
}
