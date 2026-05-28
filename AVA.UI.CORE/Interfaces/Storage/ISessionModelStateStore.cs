using AVA.UI.CORE.Models.UI;

namespace AVA.UI.CORE.Interfaces.Storage
{
    /// <summary>
    /// Persists UI-owned model-card state under the AVA sessions folder.
    /// </summary>
    public interface ISessionModelStateStore
    {
        /// <summary>
        /// Loads all saved session model state records.
        /// </summary>
        Task<IReadOnlyList<SessionModelStateRecord>> LoadAsync(CancellationToken ct = default);

        /// <summary>
        /// Saves or replaces the model state for a single workspace session.
        /// </summary>
        Task SaveAsync(SessionModelStateRecord record, CancellationToken ct = default);

        /// <summary>
        /// Applies saved model states to matching runtime vault sessions.
        /// Used as offline fallback when DB is unavailable.
        /// </summary>
        Task ApplyToVaultsAsync(IEnumerable<VaultState> vaults, CancellationToken ct = default);

        /// <summary>
        /// Applies saved model state to one matching runtime session.
        /// </summary>
        Task ApplyToSessionAsync(string vaultId, string? projectId, SessionState session, CancellationToken ct = default);

        /// <summary>
        /// Replaces the entire JSON store with records mirrored from the loaded vaults.
        /// Called after a successful DB load to keep the offline backup in sync.
        /// </summary>
        Task MirrorAsync(IEnumerable<VaultState> vaults, CancellationToken ct = default);
    }
}
