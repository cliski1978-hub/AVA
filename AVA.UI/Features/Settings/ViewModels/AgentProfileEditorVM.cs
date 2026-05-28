using AVA.UI.Features.Settings.State;

namespace AVA.UI.Features.Settings.ViewModels;

/// <summary>
/// ViewModel for AgentProfileEditor.razor.
/// Owns agent connection profile form state and save dispatch.
/// </summary>
public class AgentProfileEditorVM : IDisposable
{
    private readonly SettingsState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public AgentProfileEditorVM(SettingsState state)
    {
        _state = state;
    }

    // TODO: Draft profile properties
    // TODO: SaveAsync(), Cancel()
    // TODO: Validation

    public void Dispose() { }
}
