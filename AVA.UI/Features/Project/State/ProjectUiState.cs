namespace AVA.UI.Features.Project.State;

/// <summary>
/// Runtime display state for the Project feature.
/// Owns active project selection and project tree UI state.
/// </summary>
public class ProjectUiState
{
    // ── Event ────────────────────────────────────────────────────────────────
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Runtime State ────────────────────────────────────────────────────────
    // TODO: Extract from AppState
    // - ActiveProjectId
    // - Project CRUD dispatch
}
