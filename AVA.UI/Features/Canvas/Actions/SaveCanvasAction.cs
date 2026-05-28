namespace AVA.UI.Features.Canvas.Actions;

/// <summary>
/// Saves the current canvas state to session persistence.
/// Replaces: AppState.SaveCanvasState() called from SessionCanvas, GlobalPromptBar
/// Pipeline: SessionCanvasVM → SaveCanvasAction → CanvasRuntimeState + AvaSettingsService → Notify
/// </summary>
public class SaveCanvasAction
{
    // TODO: Inject CanvasRuntimeState, AvaSettingsService
    // TODO: ExecuteAsync()
}
