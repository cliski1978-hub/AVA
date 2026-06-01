using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using AVA.Memory.Abstractions;
using AVA.UI.CORE.Models.UI;
using AVA.UI.Vault.Mapping;
using AVA.Vault.Core.Adapters;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Data;
using AVA.Vault.Core.Services.Data.VaultProjects;

namespace AVA.UI.Vault.Services;

/// <summary>
/// Database-backed bridge between ViewModels and AVA.Vault.Core.
/// All write operations return the CfkApiResponse-derived response object directly —
/// no wrapping, no exception conversion. Succeeded and UserMessage travel intact to the VM.
/// </summary>
public class VaultUiSyncService : IVaultUiSyncService
{
    private readonly IDbContextFactory<VaultDbContext> _dbFactory;
    private readonly IVaultPersistenceProvider _dbProvider;
    private readonly IMemoryStore _memoryStore;
    private readonly VaultLogger _vaultLogger;
    private readonly ILogger<VaultUiSyncService> _logger;

    public VaultUiSyncService(
        IDbContextFactory<VaultDbContext> dbFactory,
        IVaultPersistenceProvider dbProvider,
        IMemoryStore memoryStore,
        VaultLogger vaultLogger,
        ILogger<VaultUiSyncService> logger)
    {
        _dbFactory    = dbFactory;
        _dbProvider   = dbProvider;
        _memoryStore  = memoryStore;
        _vaultLogger  = vaultLogger;
        _logger       = logger;
    }

    // ── Infrastructure ────────────────────────────────────────────────────────

    public Task EnsureVaultInfrastructureAsync()
    {
        _logger.LogInformation("VaultUiSyncService: infrastructure check complete.");
        return Task.CompletedTask;
    }

    public Task EnsureVaultExistsAsync(VaultState vaultState) => Task.CompletedTask;
    public Task EnsureProjectExistsAsync(VaultState vaultState, ProjectState projectState) => Task.CompletedTask;

    // ── Startup hydration ─────────────────────────────────────────────────────

    public async Task<List<VaultState>> LoadVaultsFromDatabaseAsync()
    {
        var result = new List<VaultState>();

        try
        {
            var headers = await _dbProvider.ListVaultsAsync();

            foreach (var header in headers)
            {
                var projects = await _dbProvider.ListProjectsAsync(header.ID);
                var sessions = await _dbProvider.ListSessionsAsync(header.ID);

                var vaultState = new VaultState
                {
                    VaultId     = header.ID,
                    Name        = header.DisplayName,
                    IsExpanded  = true
                };

                var projectIndex = new Dictionary<string, ProjectState>();
                foreach (var p in projects)
                {
                    var ps = new ProjectState
                    {
                        ProjectId  = p.ID,
                        Name       = p.Name,
                        IsExpanded = p.IsExpanded
                    };
                    projectIndex[p.ID] = ps;
                    vaultState.Projects.Add(ps);
                }

                foreach (var s in sessions)
                {
                    if (s == null)
                    {
                        continue;
                    }

                    var ss = MapToSessionState(s);
                    if (!string.IsNullOrWhiteSpace(s.ProjectID) && projectIndex.TryGetValue(s.ProjectID, out var ps))
                        ps.Sessions.Add(ss);
                    else
                        vaultState.Sessions.Add(ss);
                }

                result.Add(vaultState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VaultUiSyncService: failed to load vaults from database.");
        }

        return result;
    }

    // ── Vault ─────────────────────────────────────────────────────────────────

    public Task<CreateVaultHeaderResponse> CreateVaultAsync(string name, string? vaultId = null)
        => _dbProvider.CreateVaultAsync(name, vaultId);

    public Task<UpdateVaultHeaderResponse> RenameVaultAsync(string vaultId, string newName)
        => _dbProvider.RenameVaultAsync(vaultId, newName);

    public Task<DeleteVaultHeaderResponse> DeleteVaultAsync(string vaultId)
        => _dbProvider.DeleteVaultAsync(vaultId);

    // ── Project ───────────────────────────────────────────────────────────────

    public Task<CreateVaultProjectResponse> CreateProjectAsync(string vaultId, string name, string? projectId = null)
        => _dbProvider.CreateProjectAsync(vaultId, name, projectId);

    public Task<UpdateVaultProjectResponse> RenameProjectAsync(string vaultId, string projectId, string newName)
        => _dbProvider.RenameProjectAsync(vaultId, projectId, newName);

    public Task<DeleteVaultProjectResponse> DeleteProjectAsync(string vaultId, string projectId)
        => _dbProvider.DeleteProjectAsync(vaultId, projectId);

    // ── Session ───────────────────────────────────────────────────────────────

    public Task<CreateVaultSessionResponse> CreateSessionAsync(string vaultId, string? projectId, string name, string? sessionId = null)
        => _dbProvider.CreateSessionAsync(vaultId, projectId, name, sessionId);

    public Task<UpdateVaultSessionResponse> RenameSessionAsync(string vaultId, string sessionId, string newName)
        => _dbProvider.RenameSessionAsync(vaultId, sessionId, newName);

    public Task<UpdateVaultSessionResponse> UpdateSessionModelStateAsync(string vaultId, string sessionId, List<string> attachedModelIds, List<string> broadcastGroupIds, string? defaultModelId)
        => _dbProvider.UpdateSessionModelStateAsync(vaultId, sessionId, attachedModelIds, broadcastGroupIds, defaultModelId);

    public Task<DeleteVaultSessionResponse> DeleteSessionAsync(string vaultId, string sessionId)
        => _dbProvider.DeleteSessionAsync(vaultId, sessionId);

    // ── Note ──────────────────────────────────────────────────────────────────

    public Task<CreateVaultNoteResponse> CreateNoteAsync(string vaultId, string projectId, string title, string content, string? sessionId = null)
        => _dbProvider.CreateNoteAsync(vaultId, projectId, title, content, sessionId);

    public Task<VaultNote?> GetNoteAsync(string vaultId, string noteId)
        => _dbProvider.GetNoteAsync(vaultId, noteId);

    public Task<UpdateVaultNoteResponse> UpdateNoteAsync(string vaultId, string noteId, string? title, string? content)
        => _dbProvider.UpdateNoteAsync(vaultId, noteId, title, content);

    public Task<DeleteVaultNoteResponse> DeleteNoteAsync(string vaultId, string noteId)
        => _dbProvider.DeleteNoteAsync(vaultId, noteId);

    // ── Tag ───────────────────────────────────────────────────────────────────

    public Task<CreateVaultTagResponse> CreateTagAsync(string vaultId, string name, string? color = null)
        => _dbProvider.CreateTagAsync(vaultId, name, color);

    public Task<DeleteVaultTagResponse> DeleteTagAsync(string vaultId, string tagId)
        => _dbProvider.DeleteTagAsync(vaultId, tagId);

    public async Task<IEnumerable<VaultTag>> ListTagsAsync(string vaultId)
    {
        try { return await _dbProvider.ListTagsAsync(vaultId); }
        catch (Exception ex) { _logger.LogError(ex, "ListTagsAsync failed [{VaultId}]", vaultId); return Enumerable.Empty<VaultTag>(); }
    }

    public Task<AssignTagToNoteResponse> AssignTagToNoteAsync(string vaultId, string noteId, string tagId)
        => _dbProvider.AssignTagToNoteAsync(vaultId, noteId, tagId);

    public Task<RemoveTagFromNoteResponse> RemoveTagFromNoteAsync(string vaultId, string noteId, string tagId)
        => _dbProvider.RemoveTagFromNoteAsync(vaultId, noteId, tagId);

    // ── Link ──────────────────────────────────────────────────────────────────

    public Task<CreateVaultLinkResponse> CreateLinkAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string? description = null)
        => _dbProvider.CreateLinkAsync(vaultId, sourceNoteId, targetNoteId, relationType, description);

    public Task<DeleteVaultLinkResponse> DeleteLinkAsync(string vaultId, string linkId)
        => _dbProvider.DeleteLinkAsync(vaultId, linkId);

    public async Task<IEnumerable<RelatedNoteResult>> GetRelatedNotesAsync(string vaultId, string noteId, string? relationType = null)
    {
        try { return await _dbProvider.GetRelatedNotesAsync(vaultId, noteId, relationType); }
        catch (Exception ex) { _logger.LogError(ex, "GetRelatedNotesAsync failed [{NoteId}]", noteId); return Enumerable.Empty<RelatedNoteResult>(); }
    }

    // ── Search ────────────────────────────────────────────────────────────────

    public async Task<List<VaultNote>> SearchNotesAsync(
        string vaultId,
        string? projectId      = null,
        string? sessionId      = null,
        string? keyword        = null,
        string? tag            = null,
        string  sortBy         = "Updated",
        bool    sortDescending = true,
        DateTime? createdAfter  = null,
        DateTime? createdBefore = null,
        DateTime? updatedAfter  = null,
        DateTime? updatedBefore = null,
        CancellationToken ct    = default)
    {
        try
        {
            var results = await _dbProvider.SearchNotesAsync(
                vaultId, projectId, sessionId, keyword, tag,
                sortBy, sortDescending,
                createdAfter, createdBefore, updatedAfter, updatedBefore, ct);

            return results.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VaultUiSyncService: search failed for vault [{VaultId}]", vaultId);
            return new List<VaultNote>();
        }
    }

    // ── Memory sync ───────────────────────────────────────────────────────────

    public async Task PushNoteToMemoryAsync(VaultNote note, CancellationToken ct = default)
    {
        try
        {
            var record = VaultNoteMemoryMapper.ToMemoryRecord(note);
            await _memoryStore.UpsertAsync(record, ct);
            _vaultLogger.Log(nameof(VaultUiSyncService), $"Note [{note.ID}] '{note.Title}' pushed to Memory.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VaultUiSyncService: failed to push note [{NoteId}] to Memory", note.ID);
        }
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static SessionState MapToSessionState(VaultSession s) => new SessionState
    {
        SessionId         = s.ID,
        Name              = s.Name,
        CreatedAt         = s.CreatedAt,
        LastActiveAt      = s.LastActiveAt,
        IsPinned          = s.IsPinned,
        AttachedModelIds  = DeserializeIds(s.AttachedModelIdsJson),
        BroadcastGroupIds = DeserializeIds(s.BroadcastGroupIdsJson),
        DefaultModelId    = s.DefaultModelId,
        IsTemplate        = s.IsTemplate,
        TemplateName      = s.TemplateName,
        SpawnCount        = s.SpawnCount
    };

    private static List<string> DeserializeIds(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new();
        }

        try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }
}
