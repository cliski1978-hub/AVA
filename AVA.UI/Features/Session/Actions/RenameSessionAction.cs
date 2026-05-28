namespace AVA.UI.Features.Session.Actions;

/// <summary>
/// Renames an existing Session.
/// Replaces: AppState.RenameSessionAsync(), AppState.RenameActiveWorkspaceSessionAsync()
/// Pipeline: SessionNodeVM → RenameSessionAction → SessionUiState + AvaSettingsService → Notify
/// </summary>
public class RenameSessionAction
{
    // TODO: Inject SessionUiState, AvaSettingsService
    // TODO: ExecuteAsync(string vaultId, string? projectId, string sessionId, string newName)
}
