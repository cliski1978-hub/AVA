using Microsoft.Extensions.Logging;
using AVA.UI.CORE.Models.UI;
using AVA.UI.Errors;
using AVA.UI.Runtime;
using AVA.UI.State;

namespace AVA.UI.Features.Session.ViewModels;

public class SessionsLayoutVM : IDisposable
{
    private readonly AppState _appState;
    private readonly IAvaRuntimeContext _ctx;
    private readonly ILogger<SessionsLayoutVM> _logger;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    public List<SessionState> Sessions => GetScopedSessions();

    public SessionsLayoutVM(
        AppState appState,
        IAvaRuntimeContext ctx,
        ILogger<SessionsLayoutVM> logger)
    {
        _appState = appState;
        _ctx      = ctx;
        _logger   = logger;
    }

    private List<SessionState> GetScopedSessions()
    {
        if (_appState.ActiveProjectId is not null)
            return _appState.ActiveProject?.Sessions ?? new();
        if (_appState.ActiveVaultId is not null)
            return _appState.ActiveVault?.Sessions ?? new();
        return new();
    }

    public async Task CreateSessionAsync()
    {
        const string source = nameof(CreateSessionAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var vaultId   = _appState.ActiveVaultId;
            var projectId = _appState.ActiveProjectId;
            if (string.IsNullOrWhiteSpace(vaultId)) return;

            var name     = $"Session {Sessions.Count + 1}";
            var response = await _ctx.Vault.CreateSessionAsync(vaultId, projectId, name);

            if (!response.Succeeded || response.Session == null)
            {
                _ctx.Errors.AddModelErrors(response, source, "Session");
                return;
            }

            var session = MapToSessionState(response.Session);
            await _appState.CreateWorkspaceSessionAsync(vaultId, projectId, session);
            _appState.SetSelectedNavigationItem("Chat");
            Notify();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateSessionAsync: failed");
            _ctx.Errors.AddError(ex.Message, source, "Session", AppErrorSeverity.Critical);
        }
    }

    public async Task SelectSessionAsync(string sessionId)
    {
        var vaultId   = _appState.ActiveVaultId;
        var projectId = _appState.ActiveProjectId;
        if (string.IsNullOrWhiteSpace(vaultId) || string.IsNullOrWhiteSpace(sessionId)) return;

        await _appState.SelectSessionAsync(vaultId, projectId, sessionId);
        Notify();
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
