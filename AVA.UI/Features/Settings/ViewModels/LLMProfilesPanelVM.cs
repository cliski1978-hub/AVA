using AVA.UI.Features.Settings.State;

namespace AVA.UI.Features.Settings.ViewModels;

/// <summary>
/// ViewModel for LLMProfilesPanel.razor.
/// Owns LLM profile list display, edit dispatch, and test dispatch.
/// Pipeline: LLMProfilesPanel.razor → LLMProfilesPanelVM → SettingsState + ConnectProfileAction
/// </summary>
public class LLMProfilesPanelVM : IDisposable
{
    private readonly SettingsState _state;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public LLMProfilesPanelVM(SettingsState state)
    {
        _state = state;
    }

    // TODO: Profiles list
    // TODO: EditProfile(string profileId)
    // TODO: TestProfile(string profileId)
    // TODO: AddProfile()
    // TODO: DeleteProfile(string profileId)

    public void Dispose() { }
}
