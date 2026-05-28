namespace AVA.UI.Features.Canvas.State;

/// <summary>
/// Runtime display state for the Canvas feature.
/// Owns active card selection, drag/resize temp state, canvas save dispatch.
/// Separate from persisted CanvasDocument and SessionCanvasState models.
/// </summary>
public class CanvasRuntimeState
{
    // ── Event ────────────────────────────────────────────────────────────────
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Runtime State ────────────────────────────────────────────────────────
    // TODO: Extract from AppState
    // - SaveCanvasState()
    // - AddCard(), RemoveCard()
    // - AddCardToActiveSessionAsync()
    // - GetModelName()
    // - Active card selection
    // - Drag/resize transient state
}
