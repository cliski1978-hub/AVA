namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Represents a deterministic compressed summary of older prompt context.
    /// </summary>
    public class CompressedContextBlock
    {
        public string BlockId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime SourceStartTimestamp { get; set; }
        public DateTime SourceEndTimestamp { get; set; }
        public int OriginalMessageCount { get; set; }
        public int OriginalEstimatedTokens { get; set; }
        public int CompressedEstimatedTokens { get; set; }
        public string SummaryText { get; set; } = string.Empty;
        public List<string> SourceMessageIds { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
