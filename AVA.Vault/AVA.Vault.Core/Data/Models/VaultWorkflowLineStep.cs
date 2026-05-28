using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultWorkflowLineSteps")]
    public class VaultWorkflowLineStep : IdentityLinkedEntityBase
    {
        public VaultWorkflowLineStep()
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

        public bool IsRequired { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        public int StepOrder { get; set; }

        [Required]
        [MaxLength(64)]
        public string StepType { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string WorkflowLineID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(WorkflowLineID))]
        [InverseProperty(nameof(VaultWorkflowLine.Steps))]
        public virtual VaultWorkflowLine WorkflowLine { get; set; } = null!;

        [InverseProperty(nameof(VaultWorkflowLineStepFileRef.WorkflowLineStep))]
        public virtual ICollection<VaultWorkflowLineStepFileRef> WorkflowLineStepFileRefs { get; set; } = new HashSet<VaultWorkflowLineStepFileRef>();

        [InverseProperty(nameof(VaultWorkflowLineStepNote.WorkflowLineStep))]
        public virtual ICollection<VaultWorkflowLineStepNote> WorkflowLineStepNotes { get; set; } = new HashSet<VaultWorkflowLineStepNote>();

        #endregion
    }
}
