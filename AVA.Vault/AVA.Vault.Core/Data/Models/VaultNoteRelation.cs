using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultNoteRelations")]
    public class VaultNoteRelation : IdentityLinkedEntityBase
    {
        public VaultNoteRelation()
        {
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ID { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        [Required]
        [MaxLength(64)]
        public string RelationType { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public float Weight { get; set; }

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string SourceNoteID { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string TargetNoteID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(SourceNoteID))]
        [InverseProperty(nameof(VaultNote.OutgoingRelations))]
        public virtual VaultNote OutgoingNote { get; set; } = null!;

        [ForeignKey(nameof(TargetNoteID))]
        [InverseProperty(nameof(VaultNote.IncomingRelations))]
        public virtual VaultNote IncomingNote { get; set; } = null!;

        #endregion
    }
}
