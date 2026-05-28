using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultSessions")]
    public class VaultSession : IdentityLinkedEntityBase
    {
        public VaultSession()
        {
            this.SessionFileRefs = new HashSet<VaultSessionFileRef>();
            this.SessionNotes = new HashSet<VaultSessionNote>();
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ID { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string? AttachedModelIdsJson { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? BroadcastGroupIdsJson { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? CanvasJson { get; set; }

        [MaxLength(128)]
        public string? DefaultModelId { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public bool IsPinned { get; set; }

        public bool IsTemplate { get; set; }

        [Column(TypeName = "datetime")]
        public DateTime? LastActiveAt { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public int SpawnCount { get; set; }

        [MaxLength(256)]
        public string? TemplateName { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Foreign Keys

        [MaxLength(128)]
        public string? ProjectID { get; set; }

        [MaxLength(128)]
        public string? VaultID { get; set; }

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(ProjectID))]
        [InverseProperty(nameof(VaultProject.Sessions))]
        public virtual VaultProject? Project { get; set; }

        [InverseProperty(nameof(VaultSessionFileRef.Session))]
        public virtual ICollection<VaultSessionFileRef> SessionFileRefs { get; set; }

        [InverseProperty(nameof(VaultSessionNote.Session))]
        public virtual ICollection<VaultSessionNote> SessionNotes { get; set; }

        [ForeignKey(nameof(VaultID))]
        public virtual VaultHeader? Vault { get; set; }

        #endregion
    }
}