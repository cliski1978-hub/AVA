using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultWorkflowNodeNotes")]
    public class VaultWorkflowNodeNote : IdentityLinkedEntityBase
    {
        public VaultWorkflowNodeNote()
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
        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int NoteOrder { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(64)]
        public string UsageRole { get; set; } = string.Empty;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string NoteID { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string WorkflowNodeID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(NoteID))]
        [InverseProperty(nameof(VaultNote.WorkflowNodeNotes))]
        public virtual VaultNote Note { get; set; } = null!;

        [ForeignKey(nameof(WorkflowNodeID))]
        [InverseProperty(nameof(VaultWorkflowNode.WorkflowNodeNotes))]
        public virtual VaultWorkflowNode WorkflowNode { get; set; } = null!;

        #endregion
    }
}
