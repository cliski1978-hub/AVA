using System;
using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultNoteDetailsDto
    {
        public string NoteID { get; set; } = string.Empty;
        public string VaultID { get; set; } = string.Empty;
        public string? SessionID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public string? MetadataJson { get; set; }
        public string? EmbeddingJson { get; set; }
        public bool IsPinned { get; set; }
        public bool IsSynced { get; set; }
        public bool IsTemplate { get; set; }
        public string? TemplateName { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<VaultNoteMetadataDto> Metadata { get; set; } = new();
        public List<VaultNoteTagDto> Tags { get; set; } = new();
        public VaultAttachedFilesResponse Files { get; set; } = null!;
        public List<VaultNoteRelationDto> IncomingRelations { get; set; } = new();
        public List<VaultNoteRelationDto> OutgoingRelations { get; set; } = new();
    }
}
