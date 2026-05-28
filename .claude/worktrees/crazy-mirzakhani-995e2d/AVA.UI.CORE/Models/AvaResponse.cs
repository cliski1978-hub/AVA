// ─────────────────────────────────────────────────────────────────────────────
//  Class    : AvaResponse
//  Namespace: AVA.UI.CORE.Models
//  Purpose  : Represents a processed response returned by the AVA pipeline.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models
{
    public class AvaResponse
    {
        /// <summary>
        /// The primary text content of the response.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Confidence score of the response (0.0 - 1.0).
        /// </summary>
        public float Confidence { get; set; } = 1.0f;

        /// <summary>
        /// Source identifier — e.g. "core", "llm", "mock".
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Optional error message if the response represents a failure.
        /// </summary>
        public string? Error { get; set; }

        /// <summary>
        /// True if the response contains an error.
        /// </summary>
        public bool HasError => !string.IsNullOrWhiteSpace(Error);
    }
}