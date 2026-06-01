using AVA.UI.CORE.Models.UI;

namespace AVA.UI.CORE.Services
{
    /// <summary>
    /// Holds the runtime Vault workspace tree loaded from the AvaVault database.
    /// </summary>
    public class VaultWorkspaceState
    {
        /// <summary>
        /// Gets the runtime Vault tree for the current UI circuit.
        /// </summary>
        public List<VaultState> Vaults { get; private set; } = new();

        /// <summary>
        /// Replaces the runtime Vault tree with database-loaded state.
        /// </summary>
        public void SetVaults(List<VaultState> vaults)
        {
            Vaults = vaults ?? new List<VaultState>();
        }
    }
}
