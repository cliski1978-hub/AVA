using System;
using System.ComponentModel.DataAnnotations;

namespace AVA.Vault.Core.Data.Entities
{
    public class ModuleIdentity
    {
        [Key]
        public int Id { get; set; }

        [MaxLength(128)]
        public string ModuleAvaId { get; set; } = default!;

        [MaxLength(128)]
        public string ModuleName { get; set; } = "AVA.Vault";

        public DateTime RegisteredAtUtc { get; set; }
    }
}
