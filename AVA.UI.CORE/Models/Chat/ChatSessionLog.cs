namespace AVA.UI.CORE.Models.Chat
{
    /// <summary>
    /// Full recoverable chat log for a single session.
    /// Stored per session — not loaded on startup.
    /// SessionId must be set via IAvaIdService.NewSessionId().
    /// </summary>
    public class ChatSessionLog
    {
        public string SessionId { get; set; }
        public string? VaultId { get; set; }
        public string? ProjectId { get; set; }
        public string Title { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime UpdatedUtc { get; set; }
        public List<ChatSessionMessage> Messages { get; set; }

        public ChatSessionLog()
        {
            SessionId  = string.Empty; // Set via IAvaIdService.NewSessionId()
            Title      = "New Chat";
            CreatedUtc = DateTime.UtcNow;
            UpdatedUtc = DateTime.UtcNow;
            Messages   = new List<ChatSessionMessage>();
        }
    }
}
