using System;

namespace AVA.UI.Features.Canvas.Actions.Models
{
    public sealed class CanvasSaveResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime? SavedAt { get; set; }
    }
}
