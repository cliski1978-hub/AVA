using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Services.Storage;

namespace AVA.UI.Features.Navigation.State;

/// <summary>
/// Runtime state for top-level navigation.
/// Owns the selected navigation item and navigation dispatch.
/// Persists selected item to session storage.
/// </summary>
public class NavigationState
{
    private readonly ISessionStorageService _session;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── State ─────────────────────────────────────────────────────────────────
    public string SelectedNavigationItem { get; private set; } = "Chat";

    public NavigationState(ISessionStorageService session)
    {
        _session = session;
    }

    // ── Session storage ───────────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        var stored = await _session.GetAsync<string>(SessionStorageKeys.NavigationSelectedItem);
        SelectedNavigationItem = string.IsNullOrWhiteSpace(stored) ? "Chat" : stored;
        Notify();
    }

    public async Task PersistSessionAsync()
    {
        await _session.SetAsync(SessionStorageKeys.NavigationSelectedItem, SelectedNavigationItem);
    }

    // ── Mutations ─────────────────────────────────────────────────────────────
    public void SetSelectedNavigationItem(string item)
    {
        SelectedNavigationItem = string.IsNullOrWhiteSpace(item) ? "Chat" : item;
        _ = PersistSessionSafeAsync();
        Notify();
    }

    private async Task PersistSessionSafeAsync()
    {
        try { await PersistSessionAsync(); }
        catch { /* circuit may be disposed during re-render */ }
    }
}
