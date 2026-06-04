using System.Collections.Generic;

namespace AVA.Vault.Core.Models.Query
{
    /// <summary>
    /// Navigation tree DTO for the left-side vault explorer panel.
    /// Groups vaults, projects, notes, sessions, and workflows into a
    /// hierarchical structure suitable for tree-view rendering.
    /// </summary>
    public sealed class VaultNavigationResponse
    {
        public List<VaultNavGroup> Vaults { get; set; } = new();
    }

    /// <summary>
    /// Represents a single vault node in the navigation tree,
    /// including its direct children (project groups, vault-level notes and sessions).
    /// </summary>
    public sealed class VaultNavGroup
    {
        public string VaultId { get; set; } = string.Empty;
        public string VaultName { get; set; } = string.Empty;
        public List<VaultNavProjectGroup> Projects { get; set; } = new();
        public List<VaultNavItem> Notes { get; set; } = new();
        public List<VaultNavItem> Sessions { get; set; } = new();
    }

    /// <summary>
    /// Represents a project node within a vault, containing its child
    /// notes, sessions, and workflows.
    /// </summary>
    public sealed class VaultNavProjectGroup
    {
        public string ProjectId { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public List<VaultNavItem> Notes { get; set; } = new();
        public List<VaultNavItem> Sessions { get; set; } = new();
        public List<VaultNavItem> Workflows { get; set; } = new();
    }

    /// <summary>
    /// A single navigation item (note, session, or workflow).
    /// </summary>
    public sealed class VaultNavItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }
}
