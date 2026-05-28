namespace AVA.UI.Features.Canvas.Actions;

/// <summary>
/// Opens a canvas document for the active session.
/// Pipeline: SessionToolbarVM → OpenCanvasAction → CanvasRuntimeState + ICanvasDocumentService → Notify
/// </summary>
public class OpenCanvasAction
{
    // TODO: Inject CanvasRuntimeState, ICanvasDocumentService
    // TODO: ExecuteAsync(string? documentId)
}
