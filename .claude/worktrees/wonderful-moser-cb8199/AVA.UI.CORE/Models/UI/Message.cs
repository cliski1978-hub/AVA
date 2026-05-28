using System;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// A single message within a card conversation.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// "user" or "assistant"
        /// </summary>
        public string Role { get; set; } = "user";

        /// <summary>
        /// Message text content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Model profile ID that produced this message.
        /// Null for user messages.
        /// </summary>
        public string? ModelId { get; set; }

        /// <summary>
        /// Display label for the model that produced this message.
        /// </summary>
        public string? ModelLabel { get; set; }

        /// <summary>
        /// When the message was created.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Whether this message represents an error response.
        /// </summary>
        public bool IsError { get; set; }
    }
}
