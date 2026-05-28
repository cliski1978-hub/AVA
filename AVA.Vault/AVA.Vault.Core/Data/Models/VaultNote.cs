using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultNotes")]
    public class VaultNote : IdentityLinkedEntityBase
    {
        public VaultNote()
        {
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ID { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? Content { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string? EmbeddingJson { get; set; }

        public bool IsPinned { get; set; }

        public bool IsSynced { get; set; }

        public bool IsTemplate { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? MetadataJson { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(256)]
        public string? TemplateName { get; set; }

        [MaxLength(512)]
        public string? Summary { get; set; }

        [Required]
        [MaxLength(256)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string VaultID { get; set; } = string.Empty;

        [MaxLength(128)]
        public string? ProjectID { get; set; }

        [MaxLength(128)]
        public string? SessionID { get; set; }

        #endregion

        #region Navigation Properties

        [InverseProperty(nameof(VaultFileRefNote.Note))]
        public virtual ICollection<VaultFileRefNote> FileRefNotes { get; set; } = new HashSet<VaultFileRefNote>();

        [InverseProperty(nameof(VaultHeaderNote.Note))]
        public virtual ICollection<VaultHeaderNote> HeaderNotes { get; set; } = new HashSet<VaultHeaderNote>();

        [InverseProperty(nameof(VaultNoteRelation.IncomingNote))]
        public virtual ICollection<VaultNoteRelation> IncomingRelations { get; set; } = new HashSet<VaultNoteRelation>();

        [InverseProperty(nameof(VaultMetadata.Note))]
        public virtual ICollection<VaultMetadata> Metadata { get; set; } = new HashSet<VaultMetadata>();

        [InverseProperty(nameof(VaultNoteFileRef.Note))]
        public virtual ICollection<VaultNoteFileRef> NoteFileRefs { get; set; } = new HashSet<VaultNoteFileRef>();

        [InverseProperty(nameof(VaultNoteRelation.OutgoingNote))]
        public virtual ICollection<VaultNoteRelation> OutgoingRelations { get; set; } = new HashSet<VaultNoteRelation>();

        [ForeignKey(nameof(ProjectID))]
        [InverseProperty(nameof(VaultProject.Notes))]
        public virtual VaultProject? Project { get; set; }

        [InverseProperty(nameof(VaultProjectNote.Note))]
        public virtual ICollection<VaultProjectNote> ProjectNotes { get; set; } = new HashSet<VaultProjectNote>();

        [ForeignKey(nameof(SessionID))]
        public virtual VaultSession? Session { get; set; }

        [InverseProperty(nameof(VaultSessionNote.Note))]
        public virtual ICollection<VaultSessionNote> SessionNotes { get; set; } = new HashSet<VaultSessionNote>();

        [InverseProperty(nameof(VaultNoteVaultTag.Note))]
        public virtual ICollection<VaultNoteVaultTag> VaultNoteVaultTags { get; set; } = new HashSet<VaultNoteVaultTag>();

        [ForeignKey(nameof(VaultID))]
        public virtual VaultHeader Vault { get; set; } = null!;

        [InverseProperty(nameof(VaultWorkflowLineNote.Note))]
        public virtual ICollection<VaultWorkflowLineNote> WorkflowLineNotes { get; set; } = new HashSet<VaultWorkflowLineNote>();

        [InverseProperty(nameof(VaultWorkflowLineStepNote.Note))]
        public virtual ICollection<VaultWorkflowLineStepNote> WorkflowLineStepNotes { get; set; } = new HashSet<VaultWorkflowLineStepNote>();

        [InverseProperty(nameof(VaultWorkflowNodeNote.Note))]
        public virtual ICollection<VaultWorkflowNodeNote> WorkflowNodeNotes { get; set; } = new HashSet<VaultWorkflowNodeNote>();

        [InverseProperty(nameof(VaultWorkflowNote.Note))]
        public virtual ICollection<VaultWorkflowNote> WorkflowNotes { get; set; } = new HashSet<VaultWorkflowNote>();

        #endregion
    }
}
