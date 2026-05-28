namespace AVA.UI.Features.Canvas.Actions.Models
{
    public sealed class CanvasContentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
