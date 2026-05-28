using AVA.UI.Features.Settings.State;

namespace AVA.UI.Features.Settings.ViewModels;

/// <summary>
/// ViewModel for RemoteProfileEditor.razor.
/// Owns remote connection profile form state and save dispatch.
/// </summary>
public class RemoteProfileEditorVM : IDisposable
{
    private readonly SettingsState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public RemoteProfileEditorVM(SettingsState state)
    {
        _state = state;
    }

    // TODO: Draft profile properties
    // TODO: SaveAsync(), Cancel()
    // TODO: Validation

    public void Dispose() { }
}
