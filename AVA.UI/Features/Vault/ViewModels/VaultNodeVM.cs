using Microsoft.Extensions.Logging;

using AVA.UI.CORE.Models.UI;
using AVA.UI.Errors;
using AVA.UI.Runtime;
using AVA.UI.State;

namespace AVA.UI.Features.Vault.ViewModels;

/// <summary>
/// ViewModel for VaultNode.razor.
/// Owns local UI state: expand, menu, rename.
/// All write operations check the CfkApiResponse returned by the Vault service layer.
/// If Succeeded is false the UserMessage surfaces in the error ribbon automatically.
/// One instance created per VaultNode component instance.
/// </summary>
public class VaultNodeVM : IDisposable
{
    private readonly AppState _appState;
    private readonly IAvaRuntimeContext _ctx;
    private readonly ILogger<VaultNodeVM> _logger;
    private VaultState _vault;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public bool ShowMenu   { get; private set; }
    public bool IsRenaming { get; private set; }
    public string DraftName { get; set; } = string.Empty;
    public bool IsExpanded => _vault.IsExpanded;
    public bool SessionsExpanded { get; private set; }

    public VaultNodeVM(
        AppState appState,
        VaultState vault,
        IAvaRuntimeContext ctx,
        ILogger<VaultNodeVM> logger)
    {
        _appState = appState;
        _vault    = vault;
        _ctx      = ctx;
        _logger   = logger;
    }

    public void UpdateVault(VaultState vault) { _vault = vault; }

    // ── Local mutations ───────────────────────────────────────────────────────

    public void ToggleExpand()
    {
        if (IsRenaming) return;
        ShowMenu = false;
        _vault.IsExpanded = !_vault.IsExpanded;
        Notify();
    }

    public void ToggleMenu() { ShowMenu = !ShowMenu; Notify(); }

    public void BeginRename()
    {
        ShowMenu   = false;
        IsRenaming = true;
        DraftName  = _vault.Name;
        Notify();
    }

    public void CancelRename() { IsRenaming = false; DraftName = string.Empty; Notify(); }

    // ── Category expand ───────────────────────────────────────────────────────
    public void ToggleSessionsExpand()
    {
        SessionsExpanded = !SessionsExpanded;
        Notify();
    }

    // ── Category navigation ───────────────────────────────────────────────────
    public void NavigateToNotes()
        => _appState.NavigateToNotes(_vault.VaultId, null);

    public void NavigateToWorkflows()
        => _appState.NavigateToWorkflows(_vault.VaultId, null);

    public void NavigateToSessions()
        => _appState.NavigateToSessions(_vault.VaultId, null);

    // ── Business operations ───────────────────────────────────────────────────

    public async Task CreateProjectAsync()
    {
        const string source = nameof(CreateProjectAsync);
        _ctx.Errors.ClearSource(source);
        ShowMenu = false; IsRenaming = false;

        try
        {
            var name     = $"Project {_vault.Projects.Count + 1}";
            var response = await _ctx.Vault.CreateProjectAsync(_vault.VaultId, name);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            var project = new ProjectState
            {
                ProjectId  = response.ProjectID!,
                Name       = name,
                IsExpanded = true
            };

            _vault.IsExpanded = true;
            await _appState.CreateProjectAsync(_vault.VaultId, project);
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateProjectAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    public async Task CreateSessionAsync()
    {
        const string source = nameof(CreateSessionAsync);
        _ctx.Errors.ClearSource(source);
        ShowMenu = false; IsRenaming = false;

        try
        {
            var name     = $"Session {_vault.Sessions.Count + 1}";
            var response = await _ctx.Vault.CreateSessionAsync(_vault.VaultId, null, name);

            if (!response.Succeeded || response.Session == null)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            var session = MapToSessionState(response.Session);
            _vault.IsExpanded = true;
            SessionsExpanded = true;
            await _appState.CreateWorkspaceSessionAsync(_vault.VaultId, null, session);
            _appState.SetSelectedNavigationItem("Sessions");
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateSessionAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Session", AppErrorSeverity.Critical);
        }
    }

    public async Task<bool> SaveRenameAsync()
    {
        var trimmed = DraftName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) { CancelRename(); return false; }

        const string source = nameof(SaveRenameAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var response = await _ctx.Vault.RenameVaultAsync(_vault.VaultId, trimmed);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return false;
            }

            IsRenaming = false;
            DraftName  = string.Empty;
            await _appState.RenameVaultAsync(_vault.VaultId, trimmed);
            Notify();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveRenameAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
            return false;
        }
    }

    public async Task RemoveVaultAsync()
    {
        const string source = nameof(RemoveVaultAsync);
        _ctx.Errors.ClearSource(source);
        ShowMenu = false; IsRenaming = false;

        try
        {
            var response = await _ctx.Vault.DeleteVaultAsync(_vault.VaultId);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            await _appState.RemoveVaultAsync(_vault.VaultId);
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveVaultAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    private static SessionState MapToSessionState(AVA.Vault.Core.Data.Models.VaultSession s) => new()
    {
        SessionId    = s.ID,
        Name         = s.Name,
        CreatedAt    = s.CreatedAt,
        LastActiveAt = s.LastActiveAt,
        IsPinned     = s.IsPinned
    };

    public void Dispose() { }
}
