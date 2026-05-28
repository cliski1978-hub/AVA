using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultTags")]
    public class VaultTag : IdentityLinkedEntityBase
    {
        public VaultTag()
        {
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ID { get; set; } = string.Empty;

        [MaxLength(32)]
        public string? Color { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsArchived { get; set; }

        [MaxLength(512)]
        public string? Metadata { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string ProjectID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [InverseProperty(nameof(VaultNoteVaultTag.Tag))]
        public virtual ICollection<VaultNoteVaultTag> VaultNoteVaultTags { get; set; } = new HashSet<VaultNoteVaultTag>();

        [ForeignKey(nameof(ProjectID))]
        [InverseProperty(nameof(VaultProject.Tags))]
        public virtual VaultProject Project { get; set; } = null!;

        #endregion
    }
}
