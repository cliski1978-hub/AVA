using System.Collections.Generic;

namespace AVA.UI.Features.Canvas.Actions.Models
{
    public sealed class CanvasActionResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public List<CanvasDocumentRef> Documents { get; set; } = new();
    }
}
