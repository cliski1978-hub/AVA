namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Defines the supported block types for a canvas document.
    /// </summary>
    public enum CanvasBlockType
    {
        /// <summary>
        /// A heading block.
        /// </summary>
        Heading = 0,

        /// <summary>
        /// A freeform text block.
        /// </summary>
        Text,

        /// <summary>
        /// A code block.
        /// </summary>
        Code,

        /// <summary>
        /// A chat-oriented transcript or exchange block.
        /// </summary>
        Chat
    }
}
