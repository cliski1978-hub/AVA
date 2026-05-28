namespace AVA.UI.Features.Canvas.Actions;

/// <summary>
/// Adds a new card to the active session canvas.
/// Replaces: AppState.AddCardToActiveSessionAsync(), AppState.AddCard()
/// Pipeline: SessionToolbarVM → CreateCardAction → CanvasRuntimeState + AvaSettingsService → Notify
/// </summary>
public class CreateCardAction
{
    // TODO: Inject CanvasRuntimeState, AvaSettingsService
    // TODO: ExecuteAsync(string? modelId)
}
