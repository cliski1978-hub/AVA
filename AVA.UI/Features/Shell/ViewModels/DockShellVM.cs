using AVA.UI.CORE.Services;
using AVA.UI.Features.Chat.State;
using AVA.UI.Features.Navigation.ViewModels;
using AVA.UI.Features.Navigation.State;
using AVA.UI.State;

namespace AVA.UI.Features.Shell.ViewModels;

/// <summary>
/// ViewModel for DockShell.razor.
/// Owns shell startup initialization: settings load, workspace hydration,
/// and session storage restore for all feature ViewModels.
/// Singleton — one initialization per app lifetime.
/// </summary>
public class DockShellVM : IDisposable
{
    private readonly AppState _appState;
    private readonly AvaSettingsService _settings;
    private readonly VaultWorkspaceFileService _vaultWorkspace;
    private readonly LeftNavVM _leftNavVM;
    private readonly NavigationState _navState;
    private readonly ChatConversationState _chatState;

    public event Action? OnChange;

    public DockShellVM(
        AppState appState,
        AvaSettingsService settings,
        VaultWorkspaceFileService vaultWorkspace,
        LeftNavVM leftNavVM,
        NavigationState navState,
        ChatConversationState chatState)
    {
        _appState       = appState;
        _settings       = settings;
        _vaultWorkspace = vaultWorkspace;
        _leftNavVM      = leftNavVM;
        _navState       = navState;
        _chatState      = chatState;
    }

    // ── Initialization ────────────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        await _settings.LoadAsync();
        using var profileLoadTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(8));
        await _appState.LoadProfilesFromVaultAsync(profileLoadTimeout.Token);
        await _vaultWorkspace.LoadAsync();
        await _appState.LoadWorkspaceStateAsync();

        // Restore session storage for all feature ViewModels.
        await _leftNavVM.InitializeAsync();
        await _navState.InitializeAsync();

        // 5.4: Load chat session index only — never auto-load full session logs.
        await _chatState.LoadSessionIndexAsync();

        _appState.NotifyStateChanged();
    }

    public void Dispose() { }
}
