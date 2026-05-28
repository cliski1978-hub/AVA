using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultHeaders")]
    public class VaultHeader
    {
        public VaultHeader()
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
        [MaxLength(256)]
        public string DisplayName { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? LastSyncedAt { get; set; }

        [MaxLength(128)]
        public string? OwnerId { get; set; }

        public int SortOrder { get; set; }

        #region Navigation Properties

        [InverseProperty(nameof(VaultHeaderFileRef.Header))]
        public virtual ICollection<VaultHeaderFileRef> HeaderFileRefs { get; set; } = new HashSet<VaultHeaderFileRef>();

        [InverseProperty(nameof(VaultHeaderNote.Header))]
        public virtual ICollection<VaultHeaderNote> HeaderNotes { get; set; } = new HashSet<VaultHeaderNote>();

        [InverseProperty(nameof(VaultProject.Vault))]
        public virtual ICollection<VaultProject> Projects { get; set; } = new HashSet<VaultProject>();

        #endregion
    }
}
