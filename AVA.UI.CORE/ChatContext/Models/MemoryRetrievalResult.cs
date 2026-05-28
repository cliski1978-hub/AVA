namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Represents externally retrieved memory context for a prompt build.
    /// </summary>
    public class MemoryRetrievalResult
    {
        public string SessionId { get; set; } = string.Empty;
        public string ModelId { get; set; } = string.Empty;
        public string QueryText { get; set; } = string.Empty;
        public List<MemoryContextItem> Items { get; set; } = new();
        public bool RetrievalApplied { get; set; }
        public List<string> Warnings { get; set; } = new();
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
