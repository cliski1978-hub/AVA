namespace AVA.UI.Features.Project.Actions;

/// <summary>
/// Renames an existing Project in UI state and Vault Core.
/// Replaces: AppState.RenameProjectAsync()
/// Pipeline: ProjectNodeVM → RenameProjectAction → ProjectUiState + IVaultUiSyncService → Notify
/// </summary>
public class RenameProjectAction
{
    // TODO: Inject ProjectUiState, IVaultUiSyncService, AvaSettingsService
    // TODO: ExecuteAsync(string vaultId, string projectId, string newName)
}
