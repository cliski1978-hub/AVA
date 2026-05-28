using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultFileRefRelations")]
    public class VaultFileRefRelation : IdentityLinkedEntityBase
    {
        public VaultFileRefRelation()
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
        public string SourceFileRefID { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string TargetFileRefID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(SourceFileRefID))]
        [InverseProperty(nameof(VaultFileRef.OutgoingFileRefRelations))]
        public virtual VaultFileRef SourceFileRef { get; set; } = null!;

        [ForeignKey(nameof(TargetFileRefID))]
        [InverseProperty(nameof(VaultFileRef.IncomingFileRefRelations))]
        public virtual VaultFileRef TargetFileRef { get; set; } = null!;

        #endregion
    }
}
