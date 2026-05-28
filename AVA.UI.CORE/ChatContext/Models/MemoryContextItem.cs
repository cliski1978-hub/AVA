namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Represents one externally retrieved memory item that may be injected into a prompt.
    /// </summary>
    public class MemoryContextItem
    {
        public string MemoryId { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public double RelevanceScore { get; set; }
        public int EstimatedTokens { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
