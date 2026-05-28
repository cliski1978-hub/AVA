namespace AVA.UI.CORE.Models.Chat
{
    /// <summary>
    /// Lightweight session index entry — no message content.
    /// Loaded on startup. Full log loaded on demand only.
    /// </summary>
    public class ChatSessionIndexItem
    {
        public string SessionId { get; set; }
        public string? VaultId { get; set; }
        public string? ProjectId { get; set; }
        public string? RelativePath { get; set; }
        public string Title { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public int MessageCount { get; set; }

        public ChatSessionIndexItem()
        {
            SessionId    = string.Empty;
            Title        = string.Empty;
            CreatedUtc   = DateTime.UtcNow;
            UpdatedUtc   = DateTime.UtcNow;
            MessageCount = 0;
        }
    }
}
