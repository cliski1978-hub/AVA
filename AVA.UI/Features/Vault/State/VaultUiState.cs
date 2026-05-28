using AVA.UI.CORE.Models.UI;

namespace AVA.UI.Features.Vault.State;

/// <summary>
/// Runtime display state for the Vault feature.
/// Owns the vault list, active vault selection, and vault tree UI state.
/// Durable persistence routes through Vault Core — not settings.json.
/// </summary>
public class VaultUiState
{
    // ── Event ────────────────────────────────────────────────────────────────
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Runtime State ────────────────────────────────────────────────────────
    // TODO: Extract from AppState
    // - Vaults (List<VaultState>)
    // - ActiveVaultId
    // - Vault CRUD dispatch
    // - InitializeWorkspaceState (vault portion)
    // - LoadWorkspaceStateAsync (vault portion)
}
