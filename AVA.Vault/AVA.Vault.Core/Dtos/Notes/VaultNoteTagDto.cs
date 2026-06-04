using System;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultNoteTagDto
    {
        public string LinkID { get; set; } = string.Empty;
        public string TagID { get; set; } = string.Empty;
        public string NoteID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Color { get; set; }
        public string? Metadata { get; set; }
        public bool IsArchived { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
