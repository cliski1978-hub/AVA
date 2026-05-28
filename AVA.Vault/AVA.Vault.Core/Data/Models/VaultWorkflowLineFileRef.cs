using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("VaultWorkflowLineFileRefs")]
    public class VaultWorkflowLineFileRef : IdentityLinkedEntityBase
    {
        public VaultWorkflowLineFileRef()
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

        public int FileOrder { get; set; }

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
        public string WorkflowLineID { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(FileRefID))]
        [InverseProperty(nameof(VaultFileRef.WorkflowLineFileRefs))]
        public virtual VaultFileRef FileRef { get; set; } = null!;

        [ForeignKey(nameof(WorkflowLineID))]
        [InverseProperty(nameof(VaultWorkflowLine.WorkflowLineFileRefs))]
        public virtual VaultWorkflowLine WorkflowLine { get; set; } = null!;

        #endregion
    }
}
