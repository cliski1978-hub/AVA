using System;
using System.Collections.Generic;

namespace AVA.UI.CORE.Models.UI
{
    /// <summary>
    /// Represents a vault, the top-level container for projects and sessions.
    /// </summary>
    public class VaultState
    {
        /// <summary>
        /// Unique vault identifier.
        /// </summary>
        public string VaultId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Human-readable vault name.
        /// </summary>
        public string Name { get; set; } = "My Vault";

        /// <summary>
        /// Optional icon token or glyph for the vault.
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Optional accent color override for the vault.
        /// </summary>
        public string? AccentColor { get; set; }

        /// <summary>
        /// Whether the vault tree is expanded in the UI.
        /// </summary>
        public bool IsExpanded { get; set; } = true;

        /// <summary>
        /// Projects contained within this vault.
        /// </summary>
        public List<ProjectState> Projects { get; set; } = new();

        /// <summary>
        /// Optional storage path for the vault on disk.
        /// </summary>
        public string? StoragePath { get; set; }
    }
}
