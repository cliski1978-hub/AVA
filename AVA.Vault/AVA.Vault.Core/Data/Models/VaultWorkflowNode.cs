using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultWorkflowNodes")]
    public class VaultWorkflowNode : IdentityLinkedEntityBase
    {
        public VaultWorkflowNode()
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

        [Column(TypeName = "nvarchar(max)")]
        public string? Instructions { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? MetadataJson { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string NodeType { get; set; } = string.Empty;

        public int NodeOrder { get; set; }

        [Required]
        [MaxLength(64)]
        public string Status { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string WorkflowID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [InverseProperty(nameof(VaultWorkflowLine.TargetWorkflowNode))]
        public virtual ICollection<VaultWorkflowLine> IncomingLines { get; set; } = new HashSet<VaultWorkflowLine>();

        [InverseProperty(nameof(VaultWorkflowLine.SourceWorkflowNode))]
        public virtual ICollection<VaultWorkflowLine> OutgoingLines { get; set; } = new HashSet<VaultWorkflowLine>();

        [ForeignKey(nameof(WorkflowID))]
        [InverseProperty(nameof(VaultWorkflow.Nodes))]
        public virtual VaultWorkflow Workflow { get; set; } = null!;

        [InverseProperty(nameof(VaultWorkflowNodeFileRef.WorkflowNode))]
        public virtual ICollection<VaultWorkflowNodeFileRef> WorkflowNodeFileRefs { get; set; } = new HashSet<VaultWorkflowNodeFileRef>();

        [InverseProperty(nameof(VaultWorkflowNodeNote.WorkflowNode))]
        public virtual ICollection<VaultWorkflowNodeNote> WorkflowNodeNotes { get; set; } = new HashSet<VaultWorkflowNodeNote>();

        #endregion
    }
}
