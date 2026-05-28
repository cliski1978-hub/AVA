namespace AVA.UI.Features.Session.Actions;

/// <summary>
/// Cycles the active Session canvas layout (Bottom → Top → Floating).
/// Replaces: AppState.CycleActiveSessionLayoutAsync()
/// Pipeline: SessionToolbarVM → CycleLayoutAction → SessionUiState + AvaSettingsService → Notify
/// </summary>
public class CycleLayoutAction
{
    // TODO: Inject SessionUiState, AvaSettingsService
    // TODO: ExecuteAsync()
}
