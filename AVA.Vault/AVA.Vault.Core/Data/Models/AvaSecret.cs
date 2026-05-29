using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    /// <summary>
    /// Stores an encrypted secret payload referenced by AVA Vault records.
    /// </summary>
    [Table("AvaSecrets")]
    public class AvaSecret
    {
        /// <summary>
        /// Stable secret row identifier.
        /// </summary>
        [Key]
        [Required]
        [MaxLength(128)]
        public string SecretId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Stable logical reference stored by dependent records.
        /// </summary>
        [Required]
        [MaxLength(512)]
        public string SecretRef { get; set; } = string.Empty;

        /// <summary>
        /// Friendly secret label for diagnostics and administrative tools.
        /// </summary>
        [Required]
        [MaxLength(256)]
        public string SecretName { get; set; } = string.Empty;

        /// <summary>
        /// Secret purpose, such as ProviderApiKey or ProviderSecondarySecret.
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string SecretType { get; set; } = string.Empty;

        /// <summary>
        /// Encrypted secret value.
        /// </summary>
        [Required]
        [Column(TypeName = "nvarchar(max)")]
        public string EncryptedValue { get; set; } = string.Empty;

        /// <summary>
        /// Encryption mechanism used for the payload.
        /// </summary>
        [Required]
        [MaxLength(64)]
        public string EncryptionProvider { get; set; } = string.Empty;

        /// <summary>
        /// Optional metadata for ownership, rotation, and audit context.
        /// </summary>
        [Column(TypeName = "nvarchar(max)")]
        public string? MetadataJson { get; set; }

        /// <summary>
        /// Indicates whether the secret is active.
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// UTC timestamp when this secret was created.
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// UTC timestamp when this secret was last updated.
        /// </summary>
        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
