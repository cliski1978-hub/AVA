using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Models;

namespace AVA.Vault.Core.Interfaces
{
    /// <summary>
    /// Defines registration, discovery, and retrieval operations for Vault instances.
    /// The registry acts as the central index for all vaults accessible to the system,
    /// whether local or distributed.
    /// </summary>
    public interface IVaultRegistry
    {
        // -------------------------------------------------------------
        // Vault Management
        // -------------------------------------------------------------

        /// <summary>
        /// Registers a new vault instance within the registry.
        /// </summary>
        /// <param name="vault">The vault instance to register.</param>
        /// <param name="ct">Cancellation token.</param>
        Task RegisterVaultAsync(VaultInstance vault, CancellationToken ct = default);

        /// <summary>
        /// Unregisters a vault instance by its ID.
        /// </summary>
        /// <param name="vaultId">The unique vault identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        Task UnregisterVaultAsync(string vaultId, CancellationToken ct = default);

        // -------------------------------------------------------------
        // Discovery & Lookup
        // -------------------------------------------------------------

        /// <summary>
        /// Retrieves a specific vault by its ID.
        /// </summary>
        /// <param name="vaultId">Unique vault identifier.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The matching vault instance, or null if not found.</returns>
        Task<VaultInstance?> GetVaultAsync(string vaultId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves all known vault instances.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>All registered vaults.</returns>
        Task<IReadOnlyCollection<VaultInstance>> GetAllVaultsAsync(CancellationToken ct = default);

        // -------------------------------------------------------------
        // Persistence & Sync
        // -------------------------------------------------------------

        /// <summary>
        /// Persists the registry’s current state to storage (e.g. disk or database).
        /// </summary>
        Task SaveAsync(CancellationToken ct = default);

        /// <summary>
        /// Reloads the registry from storage.
        /// </summary>
        Task LoadAsync(CancellationToken ct = default);
    }
}
