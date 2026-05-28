namespace AVA.UI.Features.Project.Actions;

/// <summary>
/// Creates a new Project within a Vault in UI state and Vault Core.
/// Replaces: AppState.CreateProjectAsync()
/// Pipeline: ProjectNodeVM → CreateProjectAction → ProjectUiState + IVaultUiSyncService → Notify
/// </summary>
public class CreateProjectAction
{
    // TODO: Inject ProjectUiState, IVaultUiSyncService, AvaSettingsService
    // TODO: ExecuteAsync(string vaultId, string name, string? knowledgeBaseId)
}
