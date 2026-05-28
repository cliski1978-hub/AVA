using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultWorkflows")]
    public class VaultWorkflow : IdentityLinkedEntityBase
    {
        public VaultWorkflow()
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
        public string Name { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        [Required]
        [MaxLength(64)]
        public string Status { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(64)]
        public string WorkflowType { get; set; } = string.Empty;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string ProjectID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [InverseProperty(nameof(VaultWorkflowLine.Workflow))]
        public virtual ICollection<VaultWorkflowLine> Lines { get; set; } = new HashSet<VaultWorkflowLine>();

        [InverseProperty(nameof(VaultWorkflowNode.Workflow))]
        public virtual ICollection<VaultWorkflowNode> Nodes { get; set; } = new HashSet<VaultWorkflowNode>();

        [InverseProperty(nameof(VaultWorkflowFileRef.Workflow))]
        public virtual ICollection<VaultWorkflowFileRef> WorkflowFileRefs { get; set; } = new HashSet<VaultWorkflowFileRef>();

        [InverseProperty(nameof(VaultWorkflowNote.Workflow))]
        public virtual ICollection<VaultWorkflowNote> WorkflowNotes { get; set; } = new HashSet<VaultWorkflowNote>();

        [ForeignKey(nameof(ProjectID))]
        [InverseProperty(nameof(VaultProject.Workflows))]
        public virtual VaultProject Project { get; set; } = null!;

        #endregion
    }
}
