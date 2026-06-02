using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultProjects")]
    public class VaultProject : IdentityLinkedEntityBase
    {
        public VaultProject()
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

        public bool IsArchived { get; set; }

        public bool IsExpanded { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        [Required]
        [MaxLength(64)]
        public string Status { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string VaultID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [InverseProperty(nameof(VaultProjectFileRef.Project))]
        public virtual ICollection<VaultProjectFileRef> ProjectFileRefs { get; set; } = new HashSet<VaultProjectFileRef>();

        [InverseProperty(nameof(VaultProjectNote.Project))]
        public virtual ICollection<VaultProjectNote> ProjectNotes { get; set; } = new HashSet<VaultProjectNote>();

        [ForeignKey(nameof(VaultID))]
        [InverseProperty(nameof(VaultHeader.Projects))]
        public virtual VaultHeader Vault { get; set; } = null!;

        [InverseProperty(nameof(VaultFileRef.Project))]
        public virtual ICollection<VaultFileRef> FileRefs { get; set; } = new HashSet<VaultFileRef>();

        [InverseProperty(nameof(VaultGraph.Project))]
        public virtual ICollection<VaultGraph> Graphs { get; set; } = new HashSet<VaultGraph>();

        [InverseProperty(nameof(VaultSession.Project))]
        public virtual ICollection<VaultSession> Sessions { get; set; } = new HashSet<VaultSession>();

        [InverseProperty(nameof(VaultTag.Project))]
        public virtual ICollection<VaultTag> Tags { get; set; } = new HashSet<VaultTag>();

        [InverseProperty(nameof(VaultWorkflow.Project))]
        public virtual ICollection<VaultWorkflow> Workflows { get; set; } = new HashSet<VaultWorkflow>();

        #endregion
    }
}
