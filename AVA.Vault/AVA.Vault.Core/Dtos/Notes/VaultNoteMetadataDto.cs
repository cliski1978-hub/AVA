using System;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultNoteMetadataDto
    {
        public string MetadataID { get; set; } = string.Empty;
        public string NoteID { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? OwnerID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
