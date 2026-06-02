using Microsoft.Extensions.Logging;

using AVA.UI.Errors;
using AVA.UI.Runtime;
using AVA.UI.State;
using AVA.Vault.Core.Data.Models;

namespace AVA.UI.Features.Vault.ViewModels;

/// <summary>
/// ViewModel for VaultSearchPane, VaultFilterBar, VaultNoteGrid.
/// Owns all search state, filter state, sort state, and result set.
/// Calls the runtime context's Vault boundary; never touches providers directly.
/// Singleton — shared across the search UI surface.
/// </summary>
public class VaultSearchVM : IDisposable
{
    private readonly IAvaRuntimeContext _ctx;
    private readonly AppState _appState;
    private readonly ILogger<VaultSearchVM> _logger;

    public event Action? OnChange;
    private void Notify() => OnChange?.Invoke();

    // ── Search state ──────────────────────────────────────────────────────────
    public string Keyword { get; set; } = string.Empty;
    public string? FilterTag { get; set; }
    public string? FilterProjectId { get; set; }
    public string? FilterSessionId { get; set; }
    public DateTime? FilterCreatedAfter  { get; set; }
    public DateTime? FilterCreatedBefore { get; set; }
    public DateTime? FilterUpdatedAfter  { get; set; }
    public DateTime? FilterUpdatedBefore { get; set; }

    // ── Sort state ────────────────────────────────────────────────────────────
    /// <summary>"Updated" | "Created" | "Alphabetical"</summary>
    public string SortBy { get; set; } = "Updated";
    public bool SortDescending { get; set; } = true;

    // ── Result state ──────────────────────────────────────────────────────────
    public List<VaultNote> Results { get; private set; } = new();
    public bool IsLoading { get; private set; }
    public bool HasResults => Results.Any();
    public int ResultCount => Results.Count;

    public VaultSearchVM(
        IAvaRuntimeContext ctx,
        AppState appState,
        ILogger<VaultSearchVM> logger)
    {
        _ctx      = ctx;
        _appState = appState;
        _logger   = logger;
    }

    // ── Search ────────────────────────────────────────────────────────────────

    public async Task SearchAsync()
    {
        var vaultId = _appState.ActiveVaultId;
        if (string.IsNullOrWhiteSpace(vaultId)) return;

        const string source = nameof(SearchAsync);
        _ctx.Errors.ClearSource(source);
        IsLoading = true;
        Notify();

        try
        {
            Results = await _ctx.Vault.SearchNotesAsync(
                vaultId:       vaultId,
                projectId:     FilterProjectId,
                sessionId:     FilterSessionId,
                keyword:       string.IsNullOrWhiteSpace(Keyword) ? null : Keyword.Trim(),
                tag:           FilterTag,
                sortBy:        SortBy,
                sortDescending: SortDescending,
                createdAfter:  FilterCreatedAfter,
                createdBefore: FilterCreatedBefore,
                updatedAfter:  FilterUpdatedAfter,
                updatedBefore: FilterUpdatedBefore);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VaultSearchVM: search failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
            Results = new List<VaultNote>();
        }
        finally
        {
            IsLoading = false;
            Notify();
        }
    }

    // ── Create ───────────────────────────────────────────────────────────────

    public async Task CreateNoteAsync()
    {
        var vaultId   = _appState.ActiveVaultId;
        var projectId = _appState.ActiveProjectId;
        if (string.IsNullOrWhiteSpace(vaultId)) return;

        const string source = nameof(CreateNoteAsync);
        _ctx.Errors.ClearSource(source);

        try
        {
            var response    = await _ctx.Vault.CreateNoteAsync(
                vaultId, projectId, "New Note", string.Empty);

            if (!response.Succeeded)
            {
                _ctx.Errors.AddModelErrors(response, source, "Vault");
                return;
            }

            await SearchAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateNoteAsync failed");
            _ctx.Errors.AddError(ex.Message, source, "Vault", AppErrorSeverity.Critical);
        }
    }

    // ── Sort ──────────────────────────────────────────────────────────────────

    public async Task SetSortAsync(string sortBy, bool descending)
    {
        SortBy         = sortBy;
        SortDescending = descending;
        await SearchAsync();
    }

    // ── Filters ───────────────────────────────────────────────────────────────

    public async Task ClearFiltersAsync()
    {
        Keyword             = string.Empty;
        FilterTag           = null;
        FilterProjectId     = null;
        FilterSessionId     = null;
        FilterCreatedAfter  = null;
        FilterCreatedBefore = null;
        FilterUpdatedAfter  = null;
        FilterUpdatedBefore = null;
        SortBy              = "Updated";
        SortDescending      = true;
        await SearchAsync();
    }

    public void Dispose() { }
}
