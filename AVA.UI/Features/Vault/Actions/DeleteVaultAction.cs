namespace AVA.UI.Features.Vault.Actions;

/// <summary>
/// Deletes a Vault from UI state and Vault Core durable storage.
/// Replaces: AppState.RemoveVaultAsync()
/// Pipeline: VaultNodeVM → DeleteVaultAction → VaultUiState + IVaultUiSyncService → Notify
/// </summary>
public class DeleteVaultAction
{
    // TODO: Inject VaultUiState, IVaultUiSyncService, AvaSettingsService
    // TODO: ExecuteAsync(string vaultId)
}
