using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultWorkflowLines")]
    public class VaultWorkflowLine : IdentityLinkedEntityBase
    {
        public VaultWorkflowLine()
        {
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ID { get; set; } = string.Empty;

        [Column(TypeName = "nvarchar(max)")]
        public string? ConditionJson { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column(TypeName = "nvarchar(max)")]
        public string? Description { get; set; }

        public bool IsDefaultLine { get; set; }

        [Required]
        [MaxLength(64)]
        public string LineType { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        public int LineOrder { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string SourceWorkflowNodeID { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string TargetWorkflowNodeID { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string WorkflowID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(SourceWorkflowNodeID))]
        [InverseProperty(nameof(VaultWorkflowNode.OutgoingLines))]
        public virtual VaultWorkflowNode SourceWorkflowNode { get; set; } = null!;

        [InverseProperty(nameof(VaultWorkflowLineStep.WorkflowLine))]
        public virtual ICollection<VaultWorkflowLineStep> Steps { get; set; } = new HashSet<VaultWorkflowLineStep>();

        [ForeignKey(nameof(TargetWorkflowNodeID))]
        [InverseProperty(nameof(VaultWorkflowNode.IncomingLines))]
        public virtual VaultWorkflowNode TargetWorkflowNode { get; set; } = null!;

        [ForeignKey(nameof(WorkflowID))]
        [InverseProperty(nameof(VaultWorkflow.Lines))]
        public virtual VaultWorkflow Workflow { get; set; } = null!;

        [InverseProperty(nameof(VaultWorkflowLineFileRef.WorkflowLine))]
        public virtual ICollection<VaultWorkflowLineFileRef> WorkflowLineFileRefs { get; set; } = new HashSet<VaultWorkflowLineFileRef>();

        [InverseProperty(nameof(VaultWorkflowLineNote.WorkflowLine))]
        public virtual ICollection<VaultWorkflowLineNote> WorkflowLineNotes { get; set; } = new HashSet<VaultWorkflowLineNote>();

        #endregion
    }
}
