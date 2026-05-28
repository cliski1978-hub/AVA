using System;
using AVA.UI.CORE.Models.Chat;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// A single message within a card conversation.
    /// </summary>
    public class Message
    {
        /// <summary>
        /// Stable persisted message identifier.
        /// </summary>
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// "user" or "assistant"
        /// </summary>
        public string Role { get; set; } = "user";

        /// <summary>
        /// Message text content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Model profile ID this message belongs to.
        /// User messages are also tagged so model-specific threads can be shared across cards.
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

        /// <summary>
        /// Shared turn identifier used to correlate one user prompt with many model responses.
        /// </summary>
        public string? TurnId { get; set; }

        /// <summary>
        /// Tool calls attached to this message for full details-card replay.
        /// </summary>
        public List<ChatToolCallLog> ToolCalls { get; set; } = new();

        /// <summary>
        /// Provider, routing, token, latency, raw response, and other details metadata.
        /// </summary>
        public Dictionary<string, object> ResponseMetadata { get; set; } = new();

        /// <summary>
        /// True when the original live message displayed an approval card.
        /// </summary>
        public bool RequiresApproval { get; set; }

        /// <summary>
        /// Optional approval card title.
        /// </summary>
        public string? ApprovalTitle { get; set; }

        /// <summary>
        /// Optional approval card body/details.
        /// </summary>
        public string? ApprovalDetails { get; set; }

        /// <summary>
        /// Current or historical approval status.
        /// </summary>
        public string? ApprovalStatus { get; set; }
    }
}
