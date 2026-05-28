namespace AVA.UI.CORE.ChatContext.Models
{
    /// <summary>
    /// Structured export package for manually offloaded chat history.
    /// </summary>
    public class ChatOffloadPackage
    {
        /// <summary>
        /// Gets or sets the source chat session identifier.
        /// </summary>
        public string SessionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the active vault identifier, when available.
        /// </summary>
        public string? VaultId { get; set; }

        /// <summary>
        /// Gets or sets the active project identifier, when available.
        /// </summary>
        public string? ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the package source marker.
        /// </summary>
        public string Source { get; set; } = "ChatContextOffload";

        /// <summary>
        /// Gets or sets the UTC creation timestamp.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the exported messages.
        /// </summary>
        public List<ChatOffloadMessage> Messages { get; set; } = new();

        /// <summary>
        /// Gets or sets package-level metadata for future indexing.
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
