namespace AVA.UI.Features.Canvas.Actions;

/// <summary>
/// Removes a card from the active session canvas.
/// Replaces: AppState.RemoveCard()
/// Pipeline: SessionCanvasVM → DeleteCardAction → CanvasRuntimeState + AvaSettingsService → Notify
/// </summary>
public class DeleteCardAction
{
    // TODO: Inject CanvasRuntimeState, AvaSettingsService
    // TODO: ExecuteAsync(string cardId)
}
