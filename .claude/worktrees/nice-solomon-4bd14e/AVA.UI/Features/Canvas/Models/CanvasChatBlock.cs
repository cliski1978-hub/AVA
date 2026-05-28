namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Represents a chat exchange block within a canvas document.
    /// </summary>
    public sealed class CanvasChatBlock : CanvasBlock
    {
        /// <inheritdoc />
        public override CanvasBlockType BlockType => CanvasBlockType.Chat;

        /// <summary>
        /// Gets or sets the speaker or role associated with the chat block.
        /// </summary>
        public string Role { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the chat content.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }
}
