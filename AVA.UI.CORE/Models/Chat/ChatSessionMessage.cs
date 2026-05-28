namespace AVA.UI.CORE.Models.Chat
{
    /// <summary>
    /// A single fully-recoverable message including tool calls and response metadata.
    /// MessageId must be set via IAvaIdService.NewMessageId().
    /// </summary>
    public class ChatSessionMessage
    {
        public string MessageId { get; set; }
        public string Role { get; set; }
        public string Content { get; set; }
        public string ModelId { get; set; }
        public string ModelProfileId { get; set; }
        public string ModelLabel { get; set; }
        public DateTime CreatedUtc { get; set; }
        public bool IsError { get; set; }
        public string? TurnId { get; set; }

        public List<ChatToolCallLog> ToolCalls { get; set; }
        public Dictionary<string, object> ResponseMetadata { get; set; }
        public bool RequiresApproval { get; set; }
        public string? ApprovalTitle { get; set; }
        public string? ApprovalDetails { get; set; }
        public string? ApprovalStatus { get; set; }

        public ChatSessionMessage()
        {
            MessageId        = string.Empty; // Set via IAvaIdService.NewMessageId()
            Role             = string.Empty;
            Content          = string.Empty;
            ModelId          = string.Empty;
            ModelProfileId   = string.Empty;
            ModelLabel       = string.Empty;
            CreatedUtc       = DateTime.UtcNow;
            IsError          = false;
            ToolCalls        = new List<ChatToolCallLog>();
            ResponseMetadata = new Dictionary<string, object>();
        }
    }
}
