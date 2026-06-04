using System;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultAttachedFileDto
    {
        public string LinkID { get; set; } = string.Empty;
        public string FileRefID { get; set; } = string.Empty;
        public string ParentID { get; set; } = string.Empty;
        public string ParentType { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Path { get; set; }
        public string? MimeType { get; set; }
        public long? FileSizeBytes { get; set; }
        public string? ContentHash { get; set; }
        public string UsageRole { get; set; } = string.Empty;
        public string? Instructions { get; set; }
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
