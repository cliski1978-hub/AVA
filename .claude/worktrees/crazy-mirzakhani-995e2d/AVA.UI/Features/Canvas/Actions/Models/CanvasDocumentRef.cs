using System;

namespace AVA.UI.Features.Canvas.Actions.Models
{
    public sealed class CanvasDocumentRef
    {
        public string DocumentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? SavedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsDirty { get; set; }
    }
}
