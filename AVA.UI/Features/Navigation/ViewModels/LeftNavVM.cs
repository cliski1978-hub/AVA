using Microsoft.Extensions.Logging;

using AVA.UI.CORE.Interfaces.Storage;
using AVA.UI.CORE.Services.Storage;
using AVA.UI.Errors;
using AVA.UI.Runtime;
using AVA.UI.State;

namespace AVA.UI.Features.Navigation.ViewModels;

/// <summary>
/// ViewModel for LeftNav.razor.
/// Owns collapse state and inline vault creation form.
/// Vault creation routes through the runtime context's database-backed Vault boundary.
/// </summary>
public class LeftNavVM : IDisposable
{
    private readonly AppState _appState;
    private readonly IAvaRuntimeContext _ctx;
    private readonly ILogger<LeftNavVM> _logger;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Local UI state ────────────────────────────────────────────────────────
    public bool IsCollapsed { get; private set; }

    // ── Creation form state ───────────────────────────────────────────────────
    public bool IsCreating { get; private set; }
    public string NewVaultName { get; set; } = string.Empty;

    public LeftNavVM(
        AppState appState,
        IAvaRuntimeContext ctx,
        ILogger<LeftNavVM> logger)
    {
        _appState = appState;
        _ctx      = ctx;
        _logger   = logger;
    }

    // ── Session storage ───────────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        IsCollapsed = await _ctx.Storage.GetAsync<bool>(SessionStorageKeys.NavigationSidebarCollapsed);
        Notify();
    }

    public async Task PersistSessionAsync()
    {
        await _ctx.Storage.SetAsync(SessionStorageKeys.NavigationSidebarCollapsed, IsCollapsed);
    }

    // ── Collapse ──────────────────────────────────────────────────────────────
    public void ToggleCollapse()
    {
        IsCollapsed = !IsCollapsed;
        _ = PersistSessionAsync();
        Notify();
    }

    // ── Creation form ─────────────────────────────────────────────────────────
    public void BeginCreate()
    {
        NewVaultName = $"Vault {_appState.Vaults.Count + 1}";
        IsCreating   = true;
        Notify();
    }

    public void CancelCreate()
    {
        IsCreating   = false;
        NewVaultName = string.Empty;
        Notify();
    }

    public async Task ConfirmCreateAsync()
    {
        var name = NewVaultName.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;

        IsCreating = false;
        Notify();

        const string source = nameof(ConfirmCreateAsync);

        try
        {
            _ctx.Errors.ClearSource(source);

            var response = await _ctx.Vault.CreateVaultAsync(name);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            var vault = new AVA.UI.CORE.Models.UI.VaultState
            {
                VaultId     = response.Vault!.ID,
                Name        = response.Vault.DisplayName,
                IsExpanded  = true
            };

            await _appState.CreateVaultAsync(vault);
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConfirmCreateAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    public void Dispose() { }
}
