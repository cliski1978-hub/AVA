// ─────────────────────────────────────────────────────────────────────────────
//  Class    : MemoryLogEvent
//  Namespace: AVA.UI.CORE.Models
//  Purpose  : Represents a single timestamped event in the AVA memory log.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models
{
    public class MemoryLogEvent
    {
        /// <summary>
        /// When the event was recorded.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// The event message content.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional category — e.g. "prompt", "response", "system", "error".
        /// </summary>
        public string Category { get; set; } = "system";

        /// <summary>
        /// Formatted display string combining timestamp and message.
        /// </summary>
        public string Formatted => $"[{Timestamp:HH:mm:ss}] {Message}";
    }
}