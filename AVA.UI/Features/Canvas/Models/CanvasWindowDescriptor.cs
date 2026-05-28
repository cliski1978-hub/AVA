namespace AVA.UI.Features.Canvas.Models
{
    /// <summary>
    /// Describes a prepared Canvas window launch target.
    /// </summary>
    public sealed class CanvasWindowDescriptor
    {
        /// <summary>
        /// Gets or sets the encoded request payload.
        /// </summary>
        public string EncodedRequest { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the URL used to launch the Canvas window.
        /// </summary>
        public string Url { get; set; } = "/canvas";

        /// <summary>
        /// Gets or sets the display title for the new window.
        /// </summary>
        public string Title { get; set; } = "Canvas";
    }
}
