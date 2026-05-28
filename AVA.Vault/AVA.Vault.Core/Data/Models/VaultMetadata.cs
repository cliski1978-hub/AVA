using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultMetadata")]
    public class VaultMetadata : IdentityLinkedEntityBase
    {
        public VaultMetadata()
        {
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ID { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(256)]
        public string Key { get; set; } = string.Empty;

        [MaxLength(128)]
        public string? OwnerID { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Value { get; set; } = string.Empty;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string NoteID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(NoteID))]
        [InverseProperty(nameof(VaultNote.Metadata))]
        public virtual VaultNote Note { get; set; } = null!;

        #endregion
    }
}
