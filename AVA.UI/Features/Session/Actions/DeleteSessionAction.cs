namespace AVA.UI.Features.Session.Actions;

/// <summary>
/// Deletes a Session from UI state and settings persistence.
/// Replaces: AppState.RemoveSessionAsync()
/// Pipeline: SessionNodeVM → DeleteSessionAction → SessionUiState + AvaSettingsService → Notify
/// </summary>
public class DeleteSessionAction
{
    // TODO: Inject SessionUiState, AvaSettingsService
    // TODO: ExecuteAsync(string vaultId, string? projectId, string sessionId)
}
