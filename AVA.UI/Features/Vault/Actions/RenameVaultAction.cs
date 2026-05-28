namespace AVA.UI.Features.Vault.Actions;

/// <summary>
/// Renames an existing Vault in UI state and Vault Core.
/// Replaces: AppState.RenameVaultAsync()
/// Pipeline: VaultNodeVM → RenameVaultAction → VaultUiState + IVaultUiSyncService → Notify
/// </summary>
public class RenameVaultAction
{
    // TODO: Inject VaultUiState, IVaultUiSyncService, AvaSettingsService
    // TODO: ExecuteAsync(string vaultId, string newName)
}
