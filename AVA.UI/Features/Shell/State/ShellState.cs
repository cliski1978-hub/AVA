namespace AVA.UI.Features.Shell.State;

/// <summary>
/// Runtime state for the application shell.
/// Thin coordinator over DockLayoutService for dock panel layout state.
/// </summary>
public class ShellState
{
    // ── Event ────────────────────────────────────────────────────────────────
    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Runtime State ────────────────────────────────────────────────────────
    // TODO: Wire to DockLayoutService
    // - Active zone layout
    // - Panel visibility overrides
    // - Shell-level initialization dispatch
}
