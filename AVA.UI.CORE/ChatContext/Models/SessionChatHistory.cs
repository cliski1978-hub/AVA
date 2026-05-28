namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Full recoverable chat history for a single workspace session.
    /// Not loaded on startup — loaded only when the user selects a session.
    /// </summary>
    public class SessionChatHistory
    {
        public string  SessionId  { get; set; } = string.Empty;
        public string? VaultId    { get; set; }
        public string? ProjectId  { get; set; }
        public string  Title      { get; set; } = "New Session";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public List<SessionChatMessage> Messages { get; set; } = new();
    }
}
