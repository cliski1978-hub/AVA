namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Represents the result of deterministic prompt context compression.
    /// </summary>
    public class CompressionResult
    {
        public List<PromptContextItem> PreservedItems { get; set; } = new();
        public List<CompressedContextBlock> CompressedBlocks { get; set; } = new();
        public int OriginalTokenCount { get; set; }
        public int CompressedTokenCount { get; set; }
        public int TokensSaved { get; set; }
        public bool CompressionApplied { get; set; }
        public List<string> Warnings { get; set; } = new();
    }
}
