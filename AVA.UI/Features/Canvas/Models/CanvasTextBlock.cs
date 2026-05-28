namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Represents a plain text block within a canvas document.
    /// </summary>
    public sealed class CanvasTextBlock : CanvasBlock
    {
        /// <inheritdoc />
        public override CanvasBlockType BlockType => CanvasBlockType.Text;

        /// <summary>
        /// Gets or sets the text content for the block.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}
