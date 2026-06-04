using System;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultAttachedNoteDto
    {
        public string LinkID { get; set; } = string.Empty;
        public string NoteID { get; set; } = string.Empty;
        public string ParentID { get; set; } = string.Empty;
        public string ParentType { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? ContentPreview { get; set; }
        public string UsageRole { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public bool IsRequired { get; set; }
        public bool IsPinned { get; set; }
        public bool IsTemplate { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
