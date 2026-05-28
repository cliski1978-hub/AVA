using AVA.Vault.Core.Config;
using AVA.Vault.Core.Data.Models;
using System;
using System.Collections.Generic;

namespace AVA.Vault.Core.Models
{
    /// <summary>
    /// Represents a single vault instance, whether local or distributed.
    /// Each vault maintains its own notes, tags, and configuration.
    /// </summary>
    public class VaultInstance
    {
        /// <summary>
        /// Unique identifier for this vault instance.
        /// </summary>
        public string VaultID { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name for this vault (e.g., "Research Vault", "Local Memory").
        /// </summary>
        public string DisplayName { get; set; } = "Unnamed Vault";

        /// <summary>
        /// Absolute or relative path to the vault’s data directory.
        /// </summary>
        public string VaultPath { get; set; } = string.Empty;

        /// <summary>
        /// Configuration and operational settings for this vault.
        /// </summary>
        public VaultInstanceConfig Config { get; set; } = new VaultInstanceConfig();

        /// <summary>
        /// All notes stored in this vault.
        /// </summary>
        public List<VaultNote> Notes { get; set; } = new();

        /// <summary>
        /// All tags associated with this vault.
        /// </summary>
        public List<VaultTag> Tags { get; set; } = new();

        /// <summary>
        /// When the vault was last synchronized with MemoryCore.
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// Whether the vault is currently active or loaded in memory.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Optional description of this vault’s purpose or scope.
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}
