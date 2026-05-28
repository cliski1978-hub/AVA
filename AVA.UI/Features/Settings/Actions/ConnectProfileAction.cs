namespace AVA.UI.Features.Settings.Actions;

/// <summary>
/// Connects a single LLM profile and registers its session.
/// Replaces: AppState.ConnectProfileAsync()
/// Pipeline: SettingsPaneVM → ConnectProfileAction → SettingsState + SessionManager → Notify
/// </summary>
public class ConnectProfileAction
{
    // TODO: Inject SettingsState, SessionManager
    // TODO: ExecuteAsync(string profileId)
}
