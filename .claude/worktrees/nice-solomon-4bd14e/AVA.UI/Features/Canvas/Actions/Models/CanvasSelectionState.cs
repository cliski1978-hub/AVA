using System;

namespace AVA.UI.Features.Canvas.Actions.Models
{
    /// <summary>
    /// Represents the current text selection and cursor state for a Canvas document.
    /// </summary>
    public sealed class CanvasSelectionState
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public int StartIndex { get; set; }
        public int EndIndex { get; set; }
        public int CursorPosition { get; set; }
        public string SelectedText { get; set; } = string.Empty;
        public string ActiveBlockId { get; set; } = string.Empty;
        public string ActiveBlockType { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
