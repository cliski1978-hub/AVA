namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Represents a heading block within a canvas document.
    /// </summary>
    public sealed class CanvasHeadingBlock : CanvasBlock
    {
        /// <inheritdoc />
        public override CanvasBlockType BlockType => CanvasBlockType.Heading;

        /// <summary>
        /// Gets or sets the heading text.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the heading level.
        /// </summary>
        public int Level { get; set; } = 1;
    }
}
