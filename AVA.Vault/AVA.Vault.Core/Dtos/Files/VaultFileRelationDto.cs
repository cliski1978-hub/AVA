using System;

namespace AVA.Vault.Core.Dtos.Files
{
    public class VaultFileRelationDto
    {
        public string RelationID { get; set; } = string.Empty;
        public string SourceFileRefID { get; set; } = string.Empty;
        public string TargetFileRefID { get; set; } = string.Empty;
        public string RelationType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public float Weight { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
