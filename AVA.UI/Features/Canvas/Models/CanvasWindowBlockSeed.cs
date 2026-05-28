using System.Collections.Generic;

namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Represents a serializable seed block used when opening Canvas content in a separate window.
    /// </summary>
    public sealed class CanvasWindowBlockSeed
    {
        /// <summary>
        /// Gets or sets the target block type.
        /// </summary>
        public CanvasBlockType Type { get; set; } = CanvasBlockType.Text;

        /// <summary>
        /// Gets or sets the primary block content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the block sort order.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the heading level when the seed represents a heading block.
        /// </summary>
        public int? Level { get; set; }

        /// <summary>
        /// Gets or sets the code language when the seed represents a code block.
        /// </summary>
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets the role when the seed represents a chat block.
        /// </summary>
        public string? Role { get; set; }

        /// <summary>
        /// Gets or sets optional metadata associated with the block.
        /// </summary>
        public Dictionary<string, string> Meta { get; set; } = new();
    }
}
