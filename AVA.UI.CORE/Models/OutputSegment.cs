// ─────────────────────────────────────────────────────────────────────────────
//  Class    : OutputSegment
//  Namespace: AVA.UI.CORE.Models
//  Purpose  : Represents a single rendered segment of output in the AVA UI.
//             Type drives how the segment is rendered in OutputSegmentRenderer.
// ─────────────────────────────────────────────────────────────────────────────

namespace AVA.UI.CORE.Models
{
    public class OutputSegment
    {
        /// <summary>
        /// Segment type: "text", "system", "error", "link", 
        /// "image", "file", "code", "markdown"
        /// </summary>
        public string Type { get; set; } = "text";

        /// <summary>
        /// Main display content — text, filename, image URL, code string etc.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Optional metadata — URL for links, full file path, MIME type etc.
        /// </summary>
        public string? Metadata { get; set; }

        /// <summary>
        /// Optional rendering hint — e.g. "muted", "highlight", "italic".
        /// </summary>
        public string? DisplayHint { get; set; }
    }
}