using Microsoft.Extensions.Logging;

using AVA.UI.CORE.Models.UI;
using AVA.UI.Errors;
using AVA.UI.Runtime;
using AVA.UI.State;

namespace AVA.UI.Features.Session.ViewModels;

/// <summary>
/// ViewModel for SessionNode.razor.
/// Owns local UI state: menu, rename.
/// All write operations check the CfkApiResponse returned by the Vault service layer.
/// If Succeeded is false the UserMessage surfaces in the error ribbon automatically.
/// One instance created per SessionNode component instance.
/// </summary>
public class SessionNodeVM : IDisposable
{
    private readonly AppState _appState;
    private readonly IAvaRuntimeContext _ctx;
    private readonly ILogger<SessionNodeVM> _logger;
    private readonly SessionState _session;
    private readonly string _vaultId;
    private readonly string? _projectId;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public bool ShowMenu   { get; private set; }
    public bool IsRenaming { get; private set; }
    public string DraftName { get; set; } = string.Empty;
    public bool IsActive => _appState.ActiveWorkspaceSessionId == _session.SessionId;

    public SessionNodeVM(
        AppState appState,
        SessionState session,
        string vaultId,
        string? projectId,
        IAvaRuntimeContext ctx,
        ILogger<SessionNodeVM> logger)
    {
        _appState         = appState;
        _session          = session;
        _vaultId          = vaultId;
        _projectId        = projectId;
        _ctx              = ctx;
        _logger           = logger;
    }

    public void ToggleMenu() { ShowMenu = !ShowMenu; Notify(); }

    public void BeginRename()
    {
        ShowMenu   = false;
        IsRenaming = true;
        DraftName  = _session.Name;
        Notify();
    }

    public void CancelRename() { IsRenaming = false; DraftName = string.Empty; Notify(); }

    // ── Business operations ───────────────────────────────────────────────────

    public async Task SelectSessionAsync()
    {
        ShowMenu = false;
        await _appState.SelectSessionAsync(_vaultId, _projectId, _session.SessionId);
        _appState.SetSelectedNavigationItem("Chat");
        Notify();
    }

    public async Task<bool> SaveRenameAsync()
    {
        var trimmed = DraftName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) { CancelRename(); return false; }

        const string source = nameof(SaveRenameAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var response = await _ctx.Vault.RenameSessionAsync(_vaultId, _session.SessionId, trimmed);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Session");
                return false;
            }

            ShowMenu = false; IsRenaming = false; DraftName = string.Empty;
            await _appState.RenameSessionAsync(_vaultId, _projectId, _session.SessionId, trimmed);
            Notify();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveRenameAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Session", AppErrorSeverity.Critical);
            return false;
        }
    }

    public async Task RemoveSessionAsync()
    {
        const string source = nameof(RemoveSessionAsync);
        _ctx.Errors.ClearSource(source);
        ShowMenu = false; IsRenaming = false;

        try
        {
            var response = await _ctx.Vault.DeleteSessionAsync(_vaultId, _session.SessionId);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Session");
                return;
            }

            await _appState.RemoveSessionAsync(_vaultId, _projectId, _session.SessionId);
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveSessionAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Session", AppErrorSeverity.Critical);
        }
    }

    public void Dispose() { }
}
