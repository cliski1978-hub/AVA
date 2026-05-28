using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AVA.Vault.Core.Data.Models
{
    public abstract class IdentityLinkedEntityBase
    {
        [Required]
        [MaxLength(128)]
        public string PrimaryIdentityId { get; set; } = string.Empty;

        [Required]
        [MaxLength(64)]
        public string PrimaryIdentityHandle { get; set; } = string.Empty;

        [Required]
        [MaxLength(32)]
        public string PrimaryIdentityType { get; set; } = "unknown";

        [Column(TypeName = "VARBINARY(MAX)")]
        public byte[]? IdentityList { get; set; }
    }
}
