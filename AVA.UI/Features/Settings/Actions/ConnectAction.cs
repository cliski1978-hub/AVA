namespace AVA.UI.Features.Settings.Actions;

/// <summary>
/// Connects all active LLM profiles and initialises sessions.
/// Replaces: AppState.ConnectAsync() called from SettingsPane and startup
/// Pipeline: SettingsPaneVM → ConnectAction → SettingsState + SessionManager → Notify
/// </summary>
public class ConnectAction
{
    // TODO: Inject SettingsState, SessionManager, AvaSettingsService
    // TODO: ExecuteAsync()
}
