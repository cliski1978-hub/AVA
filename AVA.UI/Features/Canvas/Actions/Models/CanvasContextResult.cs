using System.Collections.Generic;

namespace AVA.UI.Features.Canvas.Actions.Models
{
    public sealed class CanvasContextResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string BlockId { get; set; } = string.Empty;
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public string Context { get; set; } = string.Empty;
        public CanvasSelectionState? Selection { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
