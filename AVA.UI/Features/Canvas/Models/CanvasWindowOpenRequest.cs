using System.Collections.Generic;

namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Represents the payload for the <c>canvas_window_open</c> tool hook.
    /// </summary>
    public sealed class CanvasWindowOpenRequest
    {
        /// <summary>
        /// Gets or sets the tool name that opened the Canvas window.
        /// </summary>
        public string ToolName { get; set; } = Tools.CanvasToolNames.CanvasWindowOpen;

        /// <summary>
        /// Gets or sets an optional source document identifier.
        /// </summary>
        public string? DocumentId { get; set; }

        /// <summary>
        /// Gets or sets the document title.
        /// </summary>
        public string Title { get; set; } = "Untitled Canvas";

        /// <summary>
        /// Gets or sets optional plain-text content for simple document creation flows.
        /// </summary>
        public string? Content { get; set; }

        /// <summary>
        /// Gets or sets pre-structured blocks for the popped-out document.
        /// </summary>
        public List<CanvasWindowBlockSeed> InitialBlocks { get; set; } = new();

        /// <summary>
        /// Gets or sets free-form metadata for the document request.
        /// </summary>
        public Dictionary<string, string> Meta { get; set; } = new();

        /// <summary>
        /// Gets or sets optional formatting hints for downstream rendering.
        /// </summary>
        public Dictionary<string, string> Formatting { get; set; } = new();

        /// <summary>
        /// Gets or sets whether a matching window should be focused if supported by the host.
        /// </summary>
        public bool FocusExisting { get; set; } = true;
    }
}
