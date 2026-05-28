namespace AVA.UI.Features.Settings.Actions;

/// <summary>
/// Tests connectivity to a configured LLM endpoint or model.
/// Replaces: AppState.TestEndpointAsync(), AppState.TestModelAsync()
/// Pipeline: SettingsPaneVM → TestEndpointAction → SettingsState + SessionManager → Notify
/// </summary>
public class TestEndpointAction
{
    // TODO: Inject SettingsState, SessionManager
    // TODO: ExecuteAsync(string profileId, string? modelId)
}
