using System;

namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Represents the common metadata shared by all canvas document blocks.
    /// </summary>
    public abstract class CanvasBlock
    {
        /// <summary>
        /// Gets or sets the unique block identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Gets the block type represented by the derived block.
        /// </summary>
        public abstract CanvasBlockType BlockType { get; }

        /// <summary>
        /// Gets or sets the sort order used to render this block inside a document.
        /// </summary>
        public int SortOrder { get; set; }

        /// <summary>
        /// Gets or sets the UTC timestamp indicating when the block was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the UTC timestamp indicating when the block was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
