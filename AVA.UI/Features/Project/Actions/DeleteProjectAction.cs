namespace AVA.UI.Features.Project.Actions;

/// <summary>
/// Deletes a Project from UI state and Vault Core.
/// Replaces: AppState.RemoveProjectAsync()
/// Pipeline: ProjectNodeVM → DeleteProjectAction → ProjectUiState + IVaultUiSyncService → Notify
/// </summary>
public class DeleteProjectAction
{
    // TODO: Inject ProjectUiState, IVaultUiSyncService, AvaSettingsService
    // TODO: ExecuteAsync(string vaultId, string projectId)
}
