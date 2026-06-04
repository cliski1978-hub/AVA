using System;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultNoteRelationDto
    {
        public string RelationID { get; set; } = string.Empty;
        public string SourceNoteID { get; set; } = string.Empty;
        public string TargetNoteID { get; set; } = string.Empty;
        public string RelationType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public float Weight { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
