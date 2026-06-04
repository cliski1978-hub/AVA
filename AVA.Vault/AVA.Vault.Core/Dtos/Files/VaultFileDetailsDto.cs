using System;
using System.Collections.Generic;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Dtos.Files
{
    public class VaultFileDetailsDto
    {
        public string FileRefID { get; set; } = string.Empty;
        public string VaultID { get; set; } = string.Empty;
        public string? ProjectID { get; set; }
        public string? SessionID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? MimeType { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? ContentHash { get; set; }
        public int FileOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public VaultAttachedNotesResponse Notes { get; set; } = null!;
        public List<VaultFileRelationDto> IncomingRelations { get; set; } = new();
        public List<VaultFileRelationDto> OutgoingRelations { get; set; } = new();
        public VaultFileUsageDto Usage { get; set; } = null!;
    }
}
