using AVA.Vault.Core.Graph;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Logger;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Provides global access to the current active VaultService and its related managers.
    /// Used by host applications to quickly retrieve the current project and graph context.
    /// </summary>
    public static class VaultServiceRegistry
    {
        public static VaultService? CurrentVault { get; private set; }
        public static VaultProjectManager? ProjectManager { get; private set; }
        public static NoteGraph? Graph { get; private set; }

        /// <summary>
        /// Initializes the global Vault registry with the provided vault.
        /// Automatically wires up the project manager and logger.
        /// </summary>
        public static void Initialize(VaultService vault)
        {
            if (vault == null)
                throw new ArgumentNullException(nameof(vault));

            // Create a contextual logger using the vault's configuration
            var logger = new VaultLogger(vault.Config);

            CurrentVault = vault;
            ProjectManager = new VaultProjectManager(vault, logger);
            Graph = vault.Graph;
        }

        /// <summary>
        /// Clears the current vault context and related managers.
        /// </summary>
        public static void Reset()
        {
            CurrentVault = null;
            ProjectManager = null;
            Graph = null;
        }
    }
}
