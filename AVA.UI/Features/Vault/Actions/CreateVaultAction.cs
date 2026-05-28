namespace AVA.UI.Features.Vault.Actions;

/// <summary>
/// Creates a new Vault in both UI state and Vault Core durable storage.
/// Replaces: AppState.CreateVaultAsync()
/// Pipeline: VaultNodeVM → CreateVaultAction → VaultUiState + IVaultUiSyncService → Notify
/// </summary>
public class CreateVaultAction
{
    // TODO: Inject VaultUiState, IVaultUiSyncService, AvaSettingsService
    // TODO: ExecuteAsync(string name, string? icon, string? accentColor)
}
