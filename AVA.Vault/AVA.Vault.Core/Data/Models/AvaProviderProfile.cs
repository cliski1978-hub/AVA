using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    [Table("AvaProviderProfiles")]
    public class AvaProviderProfile
    {
        public AvaProviderProfile()
        {
        }

        [Key]
        [Required]
        [MaxLength(128)]
        public string ProviderProfileId { get; set; } = string.Empty;

        [MaxLength(512)]
        public string? ApiKeySecretRef { get; set; }

        [MaxLength(512)]
        public string? BaseUrl { get; set; }

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(64)]
        public string? CustomProviderType { get; set; }

        [MaxLength(64)]
        public string? CustomTransportType { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? CustomHeadersAsText { get; set; }

        [Required]
        [MaxLength(32)]
        public string TransportType { get; set; } = "Http";

        public bool IsActive { get; set; }

        public bool IsDefault { get; set; }

        public bool IsEnabled { get; set; }

        public int? MaxTokens { get; set; }

        [Column(TypeName = "nvarchar(max)")]
        public string? MetadataJson { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string ProviderType { get; set; } = string.Empty;

        public int RetryCount { get; set; }

        [MaxLength(512)]
        public string? SecondarySecretRef { get; set; }

        public int SortOrder { get; set; }

        public bool SupportsStreaming { get; set; }

        public double? Temperature { get; set; }

        public int TimeoutSeconds { get; set; } = 60;

        [Required]
        [Column(TypeName = "datetime")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        #region Navigation Properties

        public virtual ICollection<AvaModelDefinition> ModelDefinitions { get; set; } = new HashSet<AvaModelDefinition>();

        #endregion
    }
}
