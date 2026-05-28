using AVA.UI.Features.Settings.Actions;
using AVA.UI.Features.Settings.State;

namespace AVA.UI.Features.Settings.ViewModels;

/// <summary>
/// ViewModel for SettingsPane.razor.
/// Owns connection mode UI, profile management, and connect/test dispatch.
/// Pipeline: SettingsPane.razor → SettingsPaneVM → Connect/Test Actions → SettingsState
/// </summary>
public class SettingsPaneVM : IDisposable
{
    private readonly SettingsState _state;
    private readonly ConnectAction _connect;
    private readonly TestEndpointAction _test;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public SettingsPaneVM(
        SettingsState state,
        ConnectAction connect,
        TestEndpointAction test)
    {
        _state = state;
        _connect = connect;
        _test = test;
    }

    // TODO: UseDirectEndpoints, UseMockCore
    // TODO: ConnectAsync()
    // TODO: TestEndpointAsync(string profileId)
    // TODO: AddLocalProfile(), AddRemoteProfile(), AddAgentProfile()
    // TODO: RemoveProfile(string profileId)

    public void Dispose() { }
}
