using Microsoft.Extensions.Logging;

using AVA.UI.CORE.Models.UI;
using AVA.UI.Errors;
using AVA.UI.Runtime;
using AVA.UI.State;

namespace AVA.UI.Features.Project.ViewModels;

/// <summary>
/// ViewModel for ProjectNode.razor.
/// Owns local UI state: expand, menu, rename.
/// All write operations check the CfkApiResponse returned by the Vault service layer.
/// If Succeeded is false the UserMessage surfaces in the error ribbon automatically.
/// One instance created per ProjectNode component instance.
/// </summary>
public class ProjectNodeVM : IDisposable
{
    private readonly AppState _appState;
    private readonly IAvaRuntimeContext _ctx;
    private readonly ILogger<ProjectNodeVM> _logger;
    private readonly ProjectState _project;
    private readonly string _vaultId;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public bool ShowMenu   { get; private set; }
    public bool IsRenaming { get; private set; }
    public string DraftName { get; set; } = string.Empty;
    public bool IsExpanded => _project.IsExpanded;

    public ProjectNodeVM(
        AppState appState,
        ProjectState project,
        string vaultId,
        IAvaRuntimeContext ctx,
        ILogger<ProjectNodeVM> logger)
    {
        _appState         = appState;
        _project          = project;
        _vaultId          = vaultId;
        _ctx              = ctx;
        _logger           = logger;
    }

    public void ToggleExpand() { ShowMenu = false; _project.IsExpanded = !_project.IsExpanded; Notify(); }
    public void ToggleMenu()   { ShowMenu = !ShowMenu; Notify(); }

    public void BeginRename()  { ShowMenu = false; IsRenaming = true; DraftName = _project.Name; Notify(); }
    public void CancelRename() { IsRenaming = false; DraftName = string.Empty; Notify(); }

    // ── Business operations ───────────────────────────────────────────────────

    public async Task AddSessionAsync()
    {
        const string source = nameof(AddSessionAsync);
        _ctx.Errors.ClearSource(source);
        ShowMenu = false; IsRenaming = false; _project.IsExpanded = true;

        try
        {
            var name     = $"Session {_project.Sessions.Count + 1}";
            var response = await _ctx.Vault.CreateSessionAsync(_vaultId, _project.ProjectId, name);

            if (!response.Succeeded || response.Session == null)
            {
                _ctx.Errors.AddModelErrors(response, source, "Session");
                return;
            }

            var session = MapToSessionState(response.Session);
            await _appState.CreateWorkspaceSessionAsync(_vaultId, _project.ProjectId, session);
            _appState.SetSelectedNavigationItem("Chat");
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddSessionAsync: failed");
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
            var response = await _ctx.Vault.RenameProjectAsync(_vaultId, _project.ProjectId, trimmed);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Project");
                return false;
            }

            IsRenaming = false; DraftName = string.Empty;
            await _appState.RenameProjectAsync(_vaultId, _project.ProjectId, trimmed);
            Notify();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveRenameAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Project", AppErrorSeverity.Critical);
            return false;
        }
    }

    public async Task RemoveProjectAsync()
    {
        const string source = nameof(RemoveProjectAsync);
        _ctx.Errors.ClearSource(source);
        ShowMenu = false; IsRenaming = false;

        try
        {
            var response = await _ctx.Vault.DeleteProjectAsync(_vaultId, _project.ProjectId);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Project");
                return;
            }

            await _appState.RemoveProjectAsync(_vaultId, _project.ProjectId);
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveProjectAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Project", AppErrorSeverity.Critical);
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
