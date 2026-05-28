using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("AvaModelDefinitions")]
    public class AvaModelDefinition
    {
        public AvaModelDefinition()
        {
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ModelDefinitionId { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? ApiKeyOverrideRef { get; set; }

        public int? ContextWindowTokens { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public double? DefaultTemperature { get; set; }

        [Required]
        [MaxLength(256)]
        public string DisplayName { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? EndpointOverride { get; set; }

        public bool IsActive { get; set; }

        public bool IsDefault { get; set; }

        public bool IsDiscovered { get; set; }

        public bool IsEnabled { get; set; }

        public int? MaxInputCharacters { get; set; }

        public int? MaxOutputTokens { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? MetadataJson { get; set; }

        [Required]
        [MaxLength(256)]
        public string ModelId { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string ModelType { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool SupportsProviderMemory { get; set; }

        public bool SupportsReasoning { get; set; }

        public bool SupportsStreaming { get; set; }

        public bool SupportsTools { get; set; }

        public bool SupportsVision { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? SystemPrompt { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Foreign Keys

        [Required]
        [MaxLength(128)]
        public string ProviderProfileId { get; set; } = string.Empty;

        #endregion

        #region Navigation Properties

        [ForeignKey(nameof(ProviderProfileId))]
        public virtual AvaProviderProfile ProviderProfile { get; set; } = null!;

        #endregion
    }
}
