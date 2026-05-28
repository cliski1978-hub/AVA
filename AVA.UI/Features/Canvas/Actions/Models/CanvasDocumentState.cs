using System;
using System.Collections.Generic;
using AVA.UI.Features.Canvas.Models;

namespace AVA.UI.Features.Canvas.Actions.Models
{
    public sealed class CanvasDocumentState
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string DocumentId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? SavedAt { get; set; }
        public bool IsDirty { get; set; }
        public CanvasDocument? Document { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
