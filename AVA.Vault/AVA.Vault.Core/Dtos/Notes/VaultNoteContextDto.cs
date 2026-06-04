using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultNoteContextDto
    {
        public string NoteID { get; set; } = string.Empty;
        public string VaultID { get; set; } = string.Empty;
        public string? SessionID { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public string? Content { get; set; }
        public bool IsPinned { get; set; }
        public bool IsTemplate { get; set; }
        public int MetadataCount { get; set; }
        public int TagCount { get; set; }
        public int FileCount { get; set; }
        public int IncomingRelationCount { get; set; }
        public int OutgoingRelationCount { get; set; }
        public int UsageCount { get; set; }
        public List<VaultNoteMetadataDto> Metadata { get; set; } = new();
        public List<VaultNoteTagDto> Tags { get; set; } = new();
        public VaultAttachedFilesResponse Files { get; set; } = null!;
        public List<VaultNoteRelationDto> IncomingRelations { get; set; } = new();
        public List<VaultNoteRelationDto> OutgoingRelations { get; set; } = new();
        public VaultNoteUsageDto Usage { get; set; } = null!;
    }
}
