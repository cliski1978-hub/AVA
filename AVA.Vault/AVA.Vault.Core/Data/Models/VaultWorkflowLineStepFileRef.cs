using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultWorkflowLineStepFileRefs")]
    public class VaultWorkflowLineStepFileRef : IdentityLinkedEntityBase
    {
        public VaultWorkflowLineStepFileRef()
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

        public int SortOrder { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        [MaxLength(64)]
        public string UsageRole { get; set; } = string.Empty;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string FileRefID { get; set; } = string.Empty;

        [Required]
        [MaxLength(128)]
        public string WorkflowLineStepID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(FileRefID))]
        [InverseProperty(nameof(VaultFileRef.WorkflowLineStepFileRefs))]
        public virtual VaultFileRef FileRef { get; set; } = null!;

        [ForeignKey(nameof(WorkflowLineStepID))]
        [InverseProperty(nameof(VaultWorkflowLineStep.WorkflowLineStepFileRefs))]
        public virtual VaultWorkflowLineStep WorkflowLineStep { get; set; } = null!;

        #endregion
    }
}
