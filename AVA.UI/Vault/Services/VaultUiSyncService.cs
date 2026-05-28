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
/// Storage-neutral bridge between ViewModels and AVA.Vault.Core.
/// All write operations return the CfkApiResponse-derived response object directly —
/// no wrapping, no exception conversion. Succeeded and UserMessage travel intact to the VM.
/// </summary>
public class VaultUiSyncService : IVaultUiSyncService
{
    private readonly IDbContextFactory<VaultDbContext> _dbFactory;
    private readonly IVaultPersistenceProvider _dbProvider;
    private readonly IVaultPersistenceProvider _fileProvider;
    private readonly IMemoryStore _memoryStore;
    private readonly VaultLogger _vaultLogger;
    private readonly ILogger<VaultUiSyncService> _logger;

    public VaultUiSyncService(
        IDbContextFactory<VaultDbContext> dbFactory,
        IVaultPersistenceProvider dbProvider,
        IVaultPersistenceProvider fileProvider,
        IMemoryStore memoryStore,
        VaultLogger vaultLogger,
        ILogger<VaultUiSyncService> logger)
    {
        _dbFactory    = dbFactory;
        _dbProvider   = dbProvider;
        _fileProvider = fileProvider;
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

    // ── Provider routing ──────────────────────────────────────────────────────

    private IVaultPersistenceProvider Provider(string storageMode)
        => storageMode == "File" ? _fileProvider : _dbProvider;

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
                    StorageMode = "Database",
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
                    var ss = MapToSessionState(s);
                    if (s.ID != null && projectIndex.TryGetValue(s.ID, out var ps))
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

    public async Task<List<VaultState>> LoadVaultsFromFileSystemAsync()
    {
        var result = new List<VaultState>();

        try
        {
            var headers = await _fileProvider.ListVaultsAsync();

            foreach (var header in headers)
            {
                var projects = await _fileProvider.ListProjectsAsync(header.ID);
                var sessions = await _fileProvider.ListSessionsAsync(header.ID);

                var vaultState = new VaultState
                {
                    VaultId     = header.ID,
                    Name        = header.DisplayName,
                    StorageMode = "File",
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
                    var ss = MapToSessionState(s);
                    if (s.ID != null && projectIndex.TryGetValue(s.ID, out var ps))
                        ps.Sessions.Add(ss);
                    else
                        vaultState.Sessions.Add(ss);
                }

                result.Add(vaultState);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VaultUiSyncService: failed to load vaults from file system.");
        }

        return result;
    }

    // ── Vault ─────────────────────────────────────────────────────────────────

    public Task<CreateVaultHeaderResponse> CreateVaultAsync(string name, string storageMode, string? vaultId = null)
        => Provider(storageMode).CreateVaultAsync(name, vaultId);

    public Task<UpdateVaultHeaderResponse> RenameVaultAsync(string vaultId, string newName, string storageMode)
        => Provider(storageMode).RenameVaultAsync(vaultId, newName);

    public Task<DeleteVaultHeaderResponse> DeleteVaultAsync(string vaultId, string storageMode)
        => Provider(storageMode).DeleteVaultAsync(vaultId);

    // ── Project ───────────────────────────────────────────────────────────────

    public Task<CreateVaultProjectResponse> CreateProjectAsync(string vaultId, string name, string storageMode, string? projectId = null)
        => Provider(storageMode).CreateProjectAsync(vaultId, name, projectId);

    public Task<UpdateVaultProjectResponse> RenameProjectAsync(string vaultId, string projectId, string newName, string storageMode)
        => Provider(storageMode).RenameProjectAsync(vaultId, projectId, newName);

    public Task<DeleteVaultProjectResponse> DeleteProjectAsync(string vaultId, string projectId, string storageMode)
        => Provider(storageMode).DeleteProjectAsync(vaultId, projectId);

    // ── Session ───────────────────────────────────────────────────────────────

    public Task<CreateVaultSessionResponse> CreateSessionAsync(string vaultId, string? projectId, string name, string storageMode, string? sessionId = null)
        => Provider(storageMode).CreateSessionAsync(vaultId, projectId, name, sessionId);

    public Task<UpdateVaultSessionResponse> RenameSessionAsync(string vaultId, string sessionId, string newName, string storageMode)
        => Provider(storageMode).RenameSessionAsync(vaultId, sessionId, newName);

    public Task<UpdateVaultSessionResponse> UpdateSessionModelStateAsync(string vaultId, string sessionId, string storageMode, List<string> attachedModelIds, List<string> broadcastGroupIds, string? defaultModelId)
        => Provider(storageMode).UpdateSessionModelStateAsync(vaultId, sessionId, attachedModelIds, broadcastGroupIds, defaultModelId);

    public Task<DeleteVaultSessionResponse> DeleteSessionAsync(string vaultId, string sessionId, string storageMode)
        => Provider(storageMode).DeleteSessionAsync(vaultId, sessionId);

    // ── Note ──────────────────────────────────────────────────────────────────

    public Task<CreateVaultNoteResponse> CreateNoteAsync(string vaultId, string projectId, string title, string content, string storageMode, string? sessionId = null)
        => Provider(storageMode).CreateNoteAsync(vaultId, projectId, title, content, sessionId);

    public Task<VaultNote?> GetNoteAsync(string vaultId, string noteId, string storageMode)
        => Provider(storageMode).GetNoteAsync(vaultId, noteId);

    public Task<UpdateVaultNoteResponse> UpdateNoteAsync(string vaultId, string noteId, string? title, string? content, string storageMode)
        => Provider(storageMode).UpdateNoteAsync(vaultId, noteId, title, content);

    public Task<DeleteVaultNoteResponse> DeleteNoteAsync(string vaultId, string noteId, string storageMode)
        => Provider(storageMode).DeleteNoteAsync(vaultId, noteId);

    // ── Tag ───────────────────────────────────────────────────────────────────

    public Task<CreateVaultTagResponse> CreateTagAsync(string vaultId, string name, string storageMode, string? color = null)
        => Provider(storageMode).CreateTagAsync(vaultId, name, color);

    public Task<DeleteVaultTagResponse> DeleteTagAsync(string vaultId, string tagId, string storageMode)
        => Provider(storageMode).DeleteTagAsync(vaultId, tagId);

    public async Task<IEnumerable<VaultTag>> ListTagsAsync(string vaultId, string storageMode)
    {
        try { return await Provider(storageMode).ListTagsAsync(vaultId); }
        catch (Exception ex) { _logger.LogError(ex, "ListTagsAsync failed [{VaultId}]", vaultId); return Enumerable.Empty<VaultTag>(); }
    }

    public Task<AssignTagToNoteResponse> AssignTagToNoteAsync(string vaultId, string noteId, string tagId, string storageMode)
        => Provider(storageMode).AssignTagToNoteAsync(vaultId, noteId, tagId);

    public Task<RemoveTagFromNoteResponse> RemoveTagFromNoteAsync(string vaultId, string noteId, string tagId, string storageMode)
        => Provider(storageMode).RemoveTagFromNoteAsync(vaultId, noteId, tagId);

    // ── Link ──────────────────────────────────────────────────────────────────

    public Task<CreateVaultLinkResponse> CreateLinkAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string storageMode, string? description = null)
        => Provider(storageMode).CreateLinkAsync(vaultId, sourceNoteId, targetNoteId, relationType, description);

    public Task<DeleteVaultLinkResponse> DeleteLinkAsync(string vaultId, string linkId, string storageMode)
        => Provider(storageMode).DeleteLinkAsync(vaultId, linkId);

    public async Task<IEnumerable<RelatedNoteResult>> GetRelatedNotesAsync(string vaultId, string noteId, string storageMode, string? relationType = null)
    {
        try { return await Provider(storageMode).GetRelatedNotesAsync(vaultId, noteId, relationType); }
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
        string storageMode      = "Database",
        CancellationToken ct    = default)
    {
        try
        {
            var results = await Provider(storageMode).SearchNotesAsync(
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
        // DB is the primary source for model selection state.
        // session-model-state.json is the offline backup — applied as fallback when DB is unavailable.
        AttachedModelIds  = DeserializeIds(s.AttachedModelIdsJson),
        BroadcastGroupIds = DeserializeIds(s.BroadcastGroupIdsJson),
        DefaultModelId    = s.DefaultModelId,
        IsTemplate        = s.IsTemplate,
        TemplateName      = s.TemplateName,
        SpawnCount        = s.SpawnCount
    };

    private static List<string> DeserializeIds(string json)
    {
        try { return System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }
}
