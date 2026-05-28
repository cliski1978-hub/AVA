namespace AVA.UI.Features.Session.Actions;

/// <summary>
/// Creates a new workspace Session within a Vault or Project.
/// Replaces: AppState.CreateWorkspaceSessionAsync()
/// Pipeline: SessionNodeVM → CreateSessionAction → SessionUiState + AvaSettingsService → Notify
/// </summary>
public class CreateSessionAction
{
    // TODO: Inject SessionUiState, AvaSettingsService
    // TODO: ExecuteAsync(string vaultId, string? projectId, string name, string? defaultModelId)
}
