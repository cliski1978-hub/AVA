namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Durable chat message model used by Sprint 3.4 persistence and consumed by Sprint 3.5 context assembly.
    /// MessageId is assigned by IAvaIdService — left empty here by default.
    /// </summary>
    public class SessionChatMessage
    {
        public string              MessageId       { get; set; } = string.Empty;
        public ChatMessageRole     Role            { get; set; } = ChatMessageRole.User;
        public DateTime            Timestamp       { get; set; } = DateTime.UtcNow;
        public string              Content         { get; set; } = string.Empty;
        public string?             ModelId         { get; set; }
        public int                 EstimatedTokens { get; set; }
        public bool                IsPinned        { get; set; }
        public bool                IsExcluded      { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
