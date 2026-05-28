namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Represents a code block within a canvas document.
    /// </summary>
    public sealed class CanvasCodeBlock : CanvasBlock
    {
        /// <inheritdoc />
        public override CanvasBlockType BlockType => CanvasBlockType.Code;

        /// <summary>
        /// Gets or sets the code content.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the optional language hint used for code rendering.
        /// </summary>
        public string Language { get; set; } = "text";
    }
}
