namespace AVA.UI.Features.Session.Actions;

/// <summary>
/// Attaches the next available LLM model to the active Session.
/// Replaces: AppState.AttachNextModelToActiveSessionAsync()
/// Pipeline: SessionToolbarVM → AttachModelAction → SessionUiState + SessionManager → Notify
/// </summary>
public class AttachModelAction
{
    // TODO: Inject SessionUiState, SessionManager, AvaSettingsService
    // TODO: ExecuteAsync()
}
