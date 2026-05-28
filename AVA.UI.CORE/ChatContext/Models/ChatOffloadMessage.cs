namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// A single chat message exported into a chat offload package.
    /// </summary>
    public class ChatOffloadMessage
    {
        /// <summary>
        /// Gets or sets the durable session message identifier.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat message role.
        /// </summary>
        public ChatMessageRole Role { get; set; } = ChatMessageRole.User;

        /// <summary>
        /// Gets or sets the UTC message timestamp.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the message content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the model identifier associated with the message.
        /// </summary>
        public string? ModelId { get; set; }

        /// <summary>
        /// Gets or sets the estimated token count.
        /// </summary>
        public int EstimatedTokens { get; set; }

        /// <summary>
        /// Gets or sets whether the message was pinned when exported.
        /// </summary>
        public bool WasPinned { get; set; }

        /// <summary>
        /// Gets or sets message-level metadata.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
