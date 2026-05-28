using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultNoteVaultTags")]
    public class VaultNoteVaultTag : IdentityLinkedEntityBase
    {
        public VaultNoteVaultTag()
        {
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ID { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int SortOrder { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string NoteID { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string TagID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(NoteID))]
        [InverseProperty(nameof(VaultNote.VaultNoteVaultTags))]
        public virtual VaultNote Note { get; set; } = null!;

        [ForeignKey(nameof(TagID))]
        [InverseProperty(nameof(VaultTag.VaultNoteVaultTags))]
        public virtual VaultTag Tag { get; set; } = null!;

        #endregion
    }
}
