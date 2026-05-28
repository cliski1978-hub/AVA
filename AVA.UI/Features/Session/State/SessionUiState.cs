namespace AVA.UI.Features.Session.State;

/// <summary>
/// Runtime display state for the Session feature.
/// Owns active session selection, session tree UI state, and session utility dispatch.
/// </summary>
public class SessionUiState
{
    // ── Event ────────────────────────────────────────────────────────────────
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Runtime State ────────────────────────────────────────────────────────
    // TODO: Extract from AppState
    // - ActiveWorkspaceSessionId
    // - Session CRUD dispatch
    // - AttachNextModelToActiveSession
    // - CycleActiveSessionLayout
    // - SaveActiveWorkspaceSession
    // - GetMostRecentSession
}
