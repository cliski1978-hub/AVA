using AVA.UI.Features.Settings.State;

namespace AVA.UI.Features.Settings.ViewModels;

/// <summary>
/// ViewModel for ConnectionProfileEditor.razor (local profiles).
/// Owns local connection profile form state and save dispatch.
/// </summary>
public class ConnectionProfileEditorVM : IDisposable
{
    private readonly SettingsState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public ConnectionProfileEditorVM(SettingsState state)
    {
        _state = state;
    }

    // TODO: Draft profile properties
    // TODO: SaveAsync(), Cancel()
    // TODO: Validation

    public void Dispose() { }
}
