using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultFileRefs")]
    public class VaultFileRef : IdentityLinkedEntityBase
    {
        public VaultFileRef()
        {
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ID { get; set; } = string.Empty;

        public long? FileSizeBytes { get; set; }

        [MaxLength(128)]
        public string? ContentHash { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(128)]
        public string? MimeType { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string Path { get; set; } = string.Empty;

        public int FileOrder { get; set; }

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

        [ForeignKey(nameof(VaultID))]
        public virtual VaultHeader Vault { get; set; } = null!;

        [ForeignKey(nameof(ProjectID))]
        [InverseProperty(nameof(VaultProject.FileRefs))]
        public virtual VaultProject? Project { get; set; }

        [ForeignKey(nameof(SessionID))]
        public virtual VaultSession? Session { get; set; }

        [InverseProperty(nameof(VaultFileRefNote.FileRef))]
        public virtual ICollection<VaultFileRefNote> FileRefNotes { get; set; } = new HashSet<VaultFileRefNote>();

        [InverseProperty(nameof(VaultHeaderFileRef.FileRef))]
        public virtual ICollection<VaultHeaderFileRef> HeaderFileRefs { get; set; } = new HashSet<VaultHeaderFileRef>();

        [InverseProperty(nameof(VaultFileRefRelation.TargetFileRef))]
        public virtual ICollection<VaultFileRefRelation> IncomingFileRefRelations { get; set; } = new HashSet<VaultFileRefRelation>();

        [InverseProperty(nameof(VaultNoteFileRef.FileRef))]
        public virtual ICollection<VaultNoteFileRef> NoteFileRefs { get; set; } = new HashSet<VaultNoteFileRef>();

        [InverseProperty(nameof(VaultFileRefRelation.SourceFileRef))]
        public virtual ICollection<VaultFileRefRelation> OutgoingFileRefRelations { get; set; } = new HashSet<VaultFileRefRelation>();

        [InverseProperty(nameof(VaultProjectFileRef.FileRef))]
        public virtual ICollection<VaultProjectFileRef> ProjectFileRefs { get; set; } = new HashSet<VaultProjectFileRef>();

        [InverseProperty(nameof(VaultSessionFileRef.FileRef))]
        public virtual ICollection<VaultSessionFileRef> SessionFileRefs { get; set; } = new HashSet<VaultSessionFileRef>();

        [InverseProperty(nameof(VaultWorkflowFileRef.FileRef))]
        public virtual ICollection<VaultWorkflowFileRef> WorkflowFileRefs { get; set; } = new HashSet<VaultWorkflowFileRef>();

        [InverseProperty(nameof(VaultWorkflowLineFileRef.FileRef))]
        public virtual ICollection<VaultWorkflowLineFileRef> WorkflowLineFileRefs { get; set; } = new HashSet<VaultWorkflowLineFileRef>();

        [InverseProperty(nameof(VaultWorkflowLineStepFileRef.FileRef))]
        public virtual ICollection<VaultWorkflowLineStepFileRef> WorkflowLineStepFileRefs { get; set; } = new HashSet<VaultWorkflowLineStepFileRef>();

        [InverseProperty(nameof(VaultWorkflowNodeFileRef.FileRef))]
        public virtual ICollection<VaultWorkflowNodeFileRef> WorkflowNodeFileRefs { get; set; } = new HashSet<VaultWorkflowNodeFileRef>();

        #endregion
    }
}
