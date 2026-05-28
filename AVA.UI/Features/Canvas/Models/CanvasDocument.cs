using System;
using System.Collections.Generic;
using System.Linq;

namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Represents a structured canvas document composed of ordered blocks.
    /// </summary>
    public sealed class CanvasDocument
    {
        /// <summary>
        /// Gets or sets the unique document identifier.
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Gets or sets the document title.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the mutable collection of blocks in the document.
        /// </summary>
        public List<CanvasBlock> Blocks { get; set; } = new();

        /// <summary>
        /// Gets the blocks ordered by sort order and then by creation time.
        /// </summary>
        public IReadOnlyList<CanvasBlock> OrderedBlocks =>
            Blocks
                .OrderBy(block => block.SortOrder)
                .ThenBy(block => block.CreatedAt)
                .ToList();

        /// <summary>
        /// Gets or sets the UTC timestamp indicating when the document was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the UTC timestamp indicating when the document was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
