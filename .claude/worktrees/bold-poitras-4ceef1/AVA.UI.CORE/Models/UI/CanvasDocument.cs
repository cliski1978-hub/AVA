using System;
using System.Collections.Generic;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Block type variants supported by the Canvas document editor.
    /// </summary>
    public enum BlockType
    {
        Title,
        Heading1,
        Heading2,
        Heading3,
        Paragraph,
        BulletItem,
        NumberedItem,
        Quote,
        Divider
    }

    /// <summary>
    /// A single content block within a CanvasDocument.
    /// </summary>
    public class DocumentBlock
    {
        /// <summary>Unique block identifier.</summary>
        public string BlockId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Visual and semantic type of the block.</summary>
        public BlockType Type { get; set; } = BlockType.Paragraph;

        /// <summary>Plain-text content of the block. Empty for Divider blocks.</summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// A structured document stored inside a session.
    /// Contains an ordered list of typed content blocks.
    /// </summary>
    public class CanvasDocument
    {
        /// <summary>Unique document identifier.</summary>
        public string DocumentId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>Display title (mirrors the Title block content).</summary>
        public string Title { get; set; } = "Untitled";

        /// <summary>Ordered list of content blocks.</summary>
        public List<DocumentBlock> Blocks { get; set; } = new();

        /// <summary>Timestamp when the document was created.</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Timestamp of the last save.</summary>
        public DateTime LastModifiedAt { get; set; } = DateTime.UtcNow;
    }
}
