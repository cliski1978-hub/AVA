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

    public Task<CreateVaultNoteResponse> CreateNoteAsync(string vaultId, string? projectId, string title, string content, string? sessionId = null)
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

    public Task<CreateVaultRelationResponse> CreateRelationAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string? description = null)
        => _dbProvider.CreateRelationAsync(vaultId, sourceNoteId, targetNoteId, relationType, description);

    public Task<DeleteVaultLinkResponse> DeleteRelationAsync(string vaultId, string linkId)
        => _dbProvider.DeleteRelationAsync(vaultId, linkId);

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

    // ── Workflow ──────────────────────────────────────────────────────────────

    public async Task<CreateVaultWorkflowResponse> CreateWorkflowAsync(string vaultId, string? projectId, string name, string? workflowId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowRequest
            {
                WorkflowID           = workflowId,
                Name                 = name,
                ProjectID            = projectId,
                Description          = null,
                Status               = null,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowResponse> UpdateWorkflowAsync(string workflowId, string? name, string? description = null, string? status = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowRequest
            {
                WorkflowID           = workflowId,
                Name                 = name,
                Description          = description,
                Status               = status,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowResponse> DeleteWorkflowAsync(string workflowId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowRequest
            {
                WorkflowID           = workflowId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── Workflow Node ─────────────────────────────────────────────────────────

    public async Task<CreateVaultWorkflowNodeResponse> CreateWorkflowNodeAsync(string workflowId, string name, string? workflowNodeId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowNodeService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowNodeRequest
            {
                WorkflowNodeID       = workflowNodeId,
                WorkflowID           = workflowId,
                Name                 = name,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowNodeResponse> UpdateWorkflowNodeAsync(string workflowNodeId, string? name = null, string? description = null, string? instructions = null, string? nodeType = null, int? nodeOrder = null, string? status = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowNodeService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowNodeRequest
            {
                WorkflowNodeID       = workflowNodeId,
                Name                 = name,
                Description          = description,
                Instructions         = instructions,
                NodeType             = nodeType,
                NodeOrder            = nodeOrder ?? 0,
                Status               = status,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowNodeResponse> DeleteWorkflowNodeAsync(string workflowNodeId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowNodeService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowNodeRequest
            {
                WorkflowNodeID       = workflowNodeId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── Workflow Line ─────────────────────────────────────────────────────────

    public async Task<CreateVaultWorkflowLineResponse> CreateWorkflowLineAsync(string workflowId, string sourceWorkflowNodeId, string targetWorkflowNodeId, string name, string? workflowLineId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowLineService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowLineRequest
            {
                WorkflowLineID       = workflowLineId,
                WorkflowID           = workflowId,
                SourceWorkflowNodeID = sourceWorkflowNodeId,
                TargetWorkflowNodeID = targetWorkflowNodeId,
                Name                 = name,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowLineResponse> UpdateWorkflowLineAsync(string workflowLineId, string? name = null, string? description = null, string? conditionJson = null, bool? isDefaultLine = null, string? lineType = null, int? lineOrder = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowLineService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowLineRequest
            {
                WorkflowLineID       = workflowLineId,
                Name                 = name,
                Description          = description,
                ConditionJson        = conditionJson,
                IsDefaultLine        = isDefaultLine ?? false,
                LineType             = lineType,
                LineOrder            = lineOrder ?? 0,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowLineResponse> DeleteWorkflowLineAsync(string workflowLineId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowLineService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowLineRequest
            {
                WorkflowLineID       = workflowLineId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── Workflow Line Step ────────────────────────────────────────────────────

    public async Task<CreateVaultWorkflowLineStepResponse> CreateWorkflowLineStepAsync(string workflowLineId, string name, string? workflowLineStepId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowLineStepService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowLineStepRequest
            {
                WorkflowLineStepID   = workflowLineStepId,
                WorkflowLineID       = workflowLineId,
                Name                 = name,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowLineStepResponse> UpdateWorkflowLineStepAsync(string workflowLineStepId, string? name = null, string? description = null, string? instructions = null, bool? isRequired = null, int? stepOrder = null, string? stepType = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowLineStepService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowLineStepRequest
            {
                WorkflowLineStepID   = workflowLineStepId,
                Name                 = name,
                Description          = description,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                StepOrder            = stepOrder ?? 0,
                StepType             = stepType,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowLineStepResponse> DeleteWorkflowLineStepAsync(string workflowLineStepId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowLineStepService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowLineStepRequest
            {
                WorkflowLineStepID   = workflowLineStepId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── Workflow Note Links ───────────────────────────────────────────────────

    public async Task<CreateVaultWorkflowNoteResponse> CreateWorkflowNoteAsync(string workflowId, string noteId, string? workflowNoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowNoteService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowNoteRequest
            {
                WorkflowNoteID       = workflowNoteId,
                WorkflowID           = workflowId,
                NoteID               = noteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowNoteResponse> UpdateWorkflowNoteAsync(string workflowNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowNoteService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowNoteRequest
            {
                WorkflowNoteID       = workflowNoteId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowNoteResponse> DeleteWorkflowNoteAsync(string workflowNoteId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowNoteService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowNoteRequest
            {
                WorkflowNoteID       = workflowNoteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultWorkflowNodeNoteResponse> CreateWorkflowNodeNoteAsync(string workflowNodeId, string noteId, string? workflowNodeNoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowNodeNoteService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowNodeNoteRequest
            {
                WorkflowNodeNoteID   = workflowNodeNoteId,
                WorkflowNodeID       = workflowNodeId,
                NoteID               = noteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowNodeNoteResponse> UpdateWorkflowNodeNoteAsync(string workflowNodeNoteId, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowNodeNoteService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowNodeNoteRequest
            {
                WorkflowNodeNoteID   = workflowNodeNoteId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                NoteOrder            = noteOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowNodeNoteResponse> DeleteWorkflowNodeNoteAsync(string workflowNodeNoteId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowNodeNoteService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowNodeNoteRequest
            {
                WorkflowNodeNoteID   = workflowNodeNoteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultWorkflowLineNoteResponse> CreateWorkflowLineNoteAsync(string workflowLineId, string noteId, string? workflowLineNoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowLineNoteService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowLineNoteRequest
            {
                WorkflowLineNoteID   = workflowLineNoteId,
                WorkflowLineID       = workflowLineId,
                NoteID               = noteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowLineNoteResponse> UpdateWorkflowLineNoteAsync(string workflowLineNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowLineNoteService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowLineNoteRequest
            {
                WorkflowLineNoteID   = workflowLineNoteId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowLineNoteResponse> DeleteWorkflowLineNoteAsync(string workflowLineNoteId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowLineNoteService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowLineNoteRequest
            {
                WorkflowLineNoteID   = workflowLineNoteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultWorkflowLineStepNoteResponse> CreateWorkflowLineStepNoteAsync(string workflowLineStepId, string noteId, string? workflowLineStepNoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowLineStepNoteService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowLineStepNoteRequest
            {
                WorkflowLineStepNoteID = workflowLineStepNoteId,
                WorkflowLineStepID     = workflowLineStepId,
                NoteID                 = noteId,
                RequestPartyName       = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowLineStepNoteResponse> UpdateWorkflowLineStepNoteAsync(string workflowLineStepNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowLineStepNoteService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowLineStepNoteRequest
            {
                WorkflowLineStepNoteID = workflowLineStepNoteId,
                Instructions           = instructions,
                IsRequired             = isRequired ?? false,
                SortOrder              = sortOrder ?? 0,
                UsageRole              = usageRole,
                RequestPartyName       = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowLineStepNoteResponse> DeleteWorkflowLineStepNoteAsync(string workflowLineStepNoteId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowLineStepNoteService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowLineStepNoteRequest
            {
                WorkflowLineStepNoteID = workflowLineStepNoteId,
                RequestPartyName       = "AVA.Vault"
            });
    }

    // ── Workflow FileRef Links ────────────────────────────────────────────────

    public async Task<CreateVaultWorkflowFileRefResponse> CreateWorkflowFileRefAsync(string workflowId, string fileRefId, string? workflowFileRefId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowFileRefService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowFileRefRequest
            {
                WorkflowFileRefID    = workflowFileRefId,
                WorkflowID           = workflowId,
                FileRefID            = fileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowFileRefResponse> UpdateWorkflowFileRefAsync(string workflowFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowFileRefService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowFileRefRequest
            {
                WorkflowFileRefID    = workflowFileRefId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowFileRefResponse> DeleteWorkflowFileRefAsync(string workflowFileRefId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowFileRefService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowFileRefRequest
            {
                WorkflowFileRefID    = workflowFileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultWorkflowNodeFileRefResponse> CreateWorkflowNodeFileRefAsync(string workflowNodeId, string fileRefId, string? workflowNodeFileRefId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowNodeFileRefService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowNodeFileRefRequest
            {
                WorkflowNodeFileRefID = workflowNodeFileRefId,
                WorkflowNodeID        = workflowNodeId,
                FileRefID             = fileRefId,
                RequestPartyName      = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowNodeFileRefResponse> UpdateWorkflowNodeFileRefAsync(string workflowNodeFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowNodeFileRefService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowNodeFileRefRequest
            {
                WorkflowNodeFileRefID = workflowNodeFileRefId,
                Instructions          = instructions,
                IsRequired            = isRequired ?? false,
                SortOrder             = sortOrder ?? 0,
                UsageRole             = usageRole,
                RequestPartyName      = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowNodeFileRefResponse> DeleteWorkflowNodeFileRefAsync(string workflowNodeFileRefId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowNodeFileRefService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowNodeFileRefRequest
            {
                WorkflowNodeFileRefID = workflowNodeFileRefId,
                RequestPartyName      = "AVA.Vault"
            });
    }

    public async Task<CreateVaultWorkflowLineFileRefResponse> CreateWorkflowLineFileRefAsync(string workflowLineId, string fileRefId, string? workflowLineFileRefId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowLineFileRefService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowLineFileRefRequest
            {
                WorkflowLineFileRefID = workflowLineFileRefId,
                WorkflowLineID        = workflowLineId,
                FileRefID             = fileRefId,
                RequestPartyName      = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowLineFileRefResponse> UpdateWorkflowLineFileRefAsync(string workflowLineFileRefId, string? instructions = null, bool? isRequired = null, int? fileOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowLineFileRefService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowLineFileRefRequest
            {
                WorkflowLineFileRefID = workflowLineFileRefId,
                Instructions          = instructions,
                IsRequired            = isRequired ?? false,
                FileOrder             = fileOrder ?? 0,
                UsageRole             = usageRole,
                RequestPartyName      = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowLineFileRefResponse> DeleteWorkflowLineFileRefAsync(string workflowLineFileRefId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowLineFileRefService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowLineFileRefRequest
            {
                WorkflowLineFileRefID = workflowLineFileRefId,
                RequestPartyName      = "AVA.Vault"
            });
    }

    public async Task<CreateVaultWorkflowLineStepFileRefResponse> CreateWorkflowLineStepFileRefAsync(string workflowLineStepId, string fileRefId, string? workflowLineStepFileRefId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultWorkflowLineStepFileRefService(adapter, _vaultLogger)
            .Execute(new CreateVaultWorkflowLineStepFileRefRequest
            {
                WorkflowLineStepFileRefID = workflowLineStepFileRefId,
                WorkflowLineStepID        = workflowLineStepId,
                FileRefID                 = fileRefId,
                RequestPartyName          = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultWorkflowLineStepFileRefResponse> UpdateWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultWorkflowLineStepFileRefService(adapter, _vaultLogger)
            .Execute(new UpdateVaultWorkflowLineStepFileRefRequest
            {
                WorkflowLineStepFileRefID = workflowLineStepFileRefId,
                Instructions              = instructions,
                IsRequired                = isRequired ?? false,
                SortOrder                 = sortOrder ?? 0,
                UsageRole                 = usageRole,
                RequestPartyName          = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultWorkflowLineStepFileRefResponse> DeleteWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultWorkflowLineStepFileRefService(adapter, _vaultLogger)
            .Execute(new DeleteVaultWorkflowLineStepFileRefRequest
            {
                WorkflowLineStepFileRefID = workflowLineStepFileRefId,
                RequestPartyName          = "AVA.Vault"
            });
    }

    // ── Vault / Project / Session Note Links ─────────────────────────────────

    public async Task<CreateVaultHeaderNoteResponse> CreateVaultHeaderNoteAsync(string vaultId, string noteId, string? headerNoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultHeaderNoteService(adapter, _vaultLogger)
            .Execute(new CreateVaultHeaderNoteRequest
            {
                HeaderNoteID         = headerNoteId,
                VaultID              = vaultId,
                NoteID               = noteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultHeaderNoteResponse> UpdateVaultHeaderNoteAsync(string headerNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultHeaderNoteService(adapter, _vaultLogger)
            .Execute(new UpdateVaultHeaderNoteRequest
            {
                HeaderNoteID         = headerNoteId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultHeaderNoteResponse> DeleteVaultHeaderNoteAsync(string headerNoteId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultHeaderNoteService(adapter, _vaultLogger)
            .Execute(new DeleteVaultHeaderNoteRequest
            {
                HeaderNoteID         = headerNoteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultProjectNoteResponse> CreateVaultProjectNoteAsync(string projectId, string noteId, string? projectNoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultProjectNoteService(adapter, _vaultLogger)
            .Execute(new CreateVaultProjectNoteRequest
            {
                ProjectNoteID        = projectNoteId,
                ProjectID            = projectId,
                NoteID               = noteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultProjectNoteResponse> UpdateVaultProjectNoteAsync(string projectNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultProjectNoteService(adapter, _vaultLogger)
            .Execute(new UpdateVaultProjectNoteRequest
            {
                ProjectNoteID        = projectNoteId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultProjectNoteResponse> DeleteVaultProjectNoteAsync(string projectNoteId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultProjectNoteService(adapter, _vaultLogger)
            .Execute(new DeleteVaultProjectNoteRequest
            {
                ProjectNoteID        = projectNoteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultSessionNoteResponse> CreateVaultSessionNoteAsync(string sessionId, string noteId, string? sessionNoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultSessionNoteService(adapter, _vaultLogger)
            .Execute(new CreateVaultSessionNoteRequest
            {
                SessionNoteID        = sessionNoteId,
                SessionID            = sessionId,
                NoteID               = noteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultSessionNoteResponse> UpdateVaultSessionNoteAsync(string sessionNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultSessionNoteService(adapter, _vaultLogger)
            .Execute(new UpdateVaultSessionNoteRequest
            {
                SessionNoteID        = sessionNoteId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultSessionNoteResponse> DeleteVaultSessionNoteAsync(string sessionNoteId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultSessionNoteService(adapter, _vaultLogger)
            .Execute(new DeleteVaultSessionNoteRequest
            {
                SessionNoteID        = sessionNoteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── Vault / Project / Session FileRef Links ──────────────────────────────

    public async Task<CreateVaultHeaderFileRefResponse> CreateVaultHeaderFileRefAsync(string vaultId, string fileRefId, string? headerFileRefId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultHeaderFileRefService(adapter, _vaultLogger)
            .Execute(new CreateVaultHeaderFileRefRequest
            {
                HeaderFileRefID      = headerFileRefId,
                VaultID              = vaultId,
                FileRefID            = fileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultHeaderFileRefResponse> UpdateVaultHeaderFileRefAsync(string headerFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultHeaderFileRefService(adapter, _vaultLogger)
            .Execute(new UpdateVaultHeaderFileRefRequest
            {
                HeaderFileRefID      = headerFileRefId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultHeaderFileRefResponse> DeleteVaultHeaderFileRefAsync(string headerFileRefId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultHeaderFileRefService(adapter, _vaultLogger)
            .Execute(new DeleteVaultHeaderFileRefRequest
            {
                HeaderFileRefID      = headerFileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultProjectFileRefResponse> CreateVaultProjectFileRefAsync(string projectId, string fileRefId, string? projectFileRefId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultProjectFileRefService(adapter, _vaultLogger)
            .Execute(new CreateVaultProjectFileRefRequest
            {
                ProjectFileRefID     = projectFileRefId,
                ProjectID            = projectId,
                FileRefID            = fileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultProjectFileRefResponse> UpdateVaultProjectFileRefAsync(string projectFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultProjectFileRefService(adapter, _vaultLogger)
            .Execute(new UpdateVaultProjectFileRefRequest
            {
                ProjectFileRefID     = projectFileRefId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultProjectFileRefResponse> DeleteVaultProjectFileRefAsync(string projectFileRefId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultProjectFileRefService(adapter, _vaultLogger)
            .Execute(new DeleteVaultProjectFileRefRequest
            {
                ProjectFileRefID     = projectFileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultSessionFileRefResponse> CreateVaultSessionFileRefAsync(string sessionId, string fileRefId, string? sessionFileRefId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultSessionFileRefService(adapter, _vaultLogger)
            .Execute(new CreateVaultSessionFileRefRequest
            {
                SessionFileRefID     = sessionFileRefId,
                SessionID            = sessionId,
                FileRefID            = fileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultSessionFileRefResponse> UpdateVaultSessionFileRefAsync(string sessionFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultSessionFileRefService(adapter, _vaultLogger)
            .Execute(new UpdateVaultSessionFileRefRequest
            {
                SessionFileRefID     = sessionFileRefId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultSessionFileRefResponse> DeleteVaultSessionFileRefAsync(string sessionFileRefId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultSessionFileRefService(adapter, _vaultLogger)
            .Execute(new DeleteVaultSessionFileRefRequest
            {
                SessionFileRefID     = sessionFileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── FileRefs ──────────────────────────────────────────────────────────────

    public async Task<CreateVaultFileRefResponse> CreateFileRefAsync(string vaultId, string name, string path, string? fileRefId = null, string? projectId = null, string? sessionId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultFileRefService(adapter, _vaultLogger)
            .Execute(new CreateVaultFileRefRequest
            {
                FileRefID            = fileRefId,
                VaultID              = vaultId,
                Name                 = name,
                Path                 = path,
                ProjectID            = projectId,
                SessionID            = sessionId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultFileRefResponse> UpdateFileRefAsync(string fileRefId, string? name = null, string? path = null, string? mimeType = null, string? contentHash = null, long? fileSizeBytes = null, int? fileOrder = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultFileRefService(adapter, _vaultLogger)
            .Execute(new UpdateVaultFileRefRequest
            {
                FileRefID            = fileRefId,
                Name                 = name,
                Path                 = path,
                MimeType             = mimeType,
                ContentHash          = contentHash,
                FileSizeBytes        = fileSizeBytes,
                FileOrder            = fileOrder ?? 0,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultFileRefResponse> DeleteFileRefAsync(string fileRefId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultFileRefService(adapter, _vaultLogger)
            .Execute(new DeleteVaultFileRefRequest
            {
                FileRefID            = fileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── FileRef Note Links ────────────────────────────────────────────────────

    public async Task<CreateVaultFileRefNoteResponse> CreateFileRefNoteAsync(string fileRefId, string noteId, string? fileRefNoteId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultFileRefNoteService(adapter, _vaultLogger)
            .Execute(new CreateVaultFileRefNoteRequest
            {
                FileRefNoteID        = fileRefNoteId,
                FileRefID            = fileRefId,
                NoteID               = noteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultFileRefNoteResponse> UpdateFileRefNoteAsync(string fileRefNoteId, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultFileRefNoteService(adapter, _vaultLogger)
            .Execute(new UpdateVaultFileRefNoteRequest
            {
                FileRefNoteID        = fileRefNoteId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                NoteOrder            = noteOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultFileRefNoteResponse> DeleteFileRefNoteAsync(string fileRefNoteId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultFileRefNoteService(adapter, _vaultLogger)
            .Execute(new DeleteVaultFileRefNoteRequest
            {
                FileRefNoteID        = fileRefNoteId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── Note FileRef Links ────────────────────────────────────────────────────

    public async Task<CreateVaultNoteFileRefResponse> CreateNoteFileRefAsync(string noteId, string fileRefId, string? noteFileRefId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultNoteFileRefService(adapter, _vaultLogger)
            .Execute(new CreateVaultNoteFileRefRequest
            {
                NoteFileRefID        = noteFileRefId,
                NoteID               = noteId,
                FileRefID            = fileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultNoteFileRefResponse> UpdateNoteFileRefAsync(string noteFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultNoteFileRefService(adapter, _vaultLogger)
            .Execute(new UpdateVaultNoteFileRefRequest
            {
                NoteFileRefID        = noteFileRefId,
                Instructions         = instructions,
                IsRequired           = isRequired ?? false,
                SortOrder            = sortOrder ?? 0,
                UsageRole            = usageRole,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultNoteFileRefResponse> DeleteNoteFileRefAsync(string noteFileRefId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultNoteFileRefService(adapter, _vaultLogger)
            .Execute(new DeleteVaultNoteFileRefRequest
            {
                NoteFileRefID        = noteFileRefId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── FileRef Relations ─────────────────────────────────────────────────────

    public async Task<CreateVaultFileRefRelationResponse> CreateFileRefRelationAsync(string sourceFileRefId, string targetFileRefId, string relationType, string? description = null, string? fileRefRelationId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultFileRefRelationService(adapter, _vaultLogger)
            .Execute(new CreateVaultFileRefRelationRequest
            {
                FileRefRelationID    = fileRefRelationId,
                SourceFileRefID      = sourceFileRefId,
                TargetFileRefID      = targetFileRefId,
                RelationType         = relationType,
                Description          = description,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultFileRefRelationResponse> UpdateFileRefRelationAsync(string fileRefRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultFileRefRelationService(adapter, _vaultLogger)
            .Execute(new UpdateVaultFileRefRelationRequest
            {
                FileRefRelationID    = fileRefRelationId,
                RelationType         = relationType,
                Description          = description,
                SortOrder            = sortOrder ?? 0,
                Weight               = weight ?? 0,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultFileRefRelationResponse> DeleteFileRefRelationAsync(string fileRefRelationId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultFileRefRelationService(adapter, _vaultLogger)
            .Execute(new DeleteVaultFileRefRelationRequest
            {
                FileRefRelationID    = fileRefRelationId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    // ── Note Relations / Metadata / Tags ──────────────────────────────────────

    public async Task<CreateVaultNoteRelationResponse> CreateNoteRelationAsync(string sourceNoteId, string targetNoteId, string relationType, string? description = null, string? noteRelationId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultNoteRelationService(adapter, _vaultLogger)
            .Execute(new CreateVaultNoteRelationRequest
            {
                NoteRelationID       = noteRelationId,
                SourceNoteID         = sourceNoteId,
                TargetNoteID         = targetNoteId,
                RelationType         = relationType,
                Description          = description,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultNoteRelationResponse> UpdateNoteRelationAsync(string noteRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultNoteRelationService(adapter, _vaultLogger)
            .Execute(new UpdateVaultNoteRelationRequest
            {
                NoteRelationID       = noteRelationId,
                RelationType         = relationType,
                Description          = description,
                SortOrder            = sortOrder ?? 0,
                Weight               = weight ?? 0,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultNoteRelationResponse> DeleteNoteRelationAsync(string noteRelationId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultNoteRelationService(adapter, _vaultLogger)
            .Execute(new DeleteVaultNoteRelationRequest
            {
                NoteRelationID       = noteRelationId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultNoteVaultTagResponse> CreateNoteVaultTagAsync(string noteId, string tagId, string? noteVaultTagId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultNoteVaultTagService(adapter, _vaultLogger)
            .Execute(new CreateVaultNoteVaultTagRequest
            {
                NoteVaultTagID       = noteVaultTagId,
                NoteID               = noteId,
                TagID                = tagId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultNoteVaultTagResponse> UpdateNoteVaultTagAsync(string noteVaultTagId, int? sortOrder = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultNoteVaultTagService(adapter, _vaultLogger)
            .Execute(new UpdateVaultNoteVaultTagRequest
            {
                NoteVaultTagID       = noteVaultTagId,
                SortOrder            = sortOrder ?? 0,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultNoteVaultTagResponse> DeleteNoteVaultTagAsync(string noteVaultTagId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultNoteVaultTagService(adapter, _vaultLogger)
            .Execute(new DeleteVaultNoteVaultTagRequest
            {
                NoteVaultTagID       = noteVaultTagId,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultTagResponse> UpdateTagAsync(string tagId, string? name = null, string? description = null, string? colorHex = null, int? sortOrder = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        var tag = adapter.Set<VaultTag>().FirstOrDefault(t => t.ID == tagId);
        return new UpdateVaultTagService(adapter, _vaultLogger)
            .Execute(new UpdateVaultTagRequest
            {
                VaultID              = tag?.ProjectID ?? string.Empty,
                TagID                = tagId,
                Name                 = name,
                Color                = colorHex,
                Description          = description,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<CreateVaultMetadataResponse> CreateMetadataAsync(string noteId, string key, string? value = null, string? metadataId = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new CreateVaultMetadataService(adapter, _vaultLogger)
            .Execute(new CreateVaultMetadataRequest
            {
                MetadataID           = metadataId,
                NoteID               = noteId,
                Key                  = key,
                Value                = value,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<UpdateVaultMetadataResponse> UpdateMetadataAsync(string metadataId, string? key = null, string? value = null)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new UpdateVaultMetadataService(adapter, _vaultLogger)
            .Execute(new UpdateVaultMetadataRequest
            {
                MetadataID           = metadataId,
                Key                  = key,
                Value                = value,
                RequestPartyName     = "AVA.Vault"
            });
    }

    public async Task<DeleteVaultMetadataResponse> DeleteMetadataAsync(string metadataId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var adapter = new VaultDbContextAdapter(db);
        return new DeleteVaultMetadataService(adapter, _vaultLogger)
            .Execute(new DeleteVaultMetadataRequest
            {
                MetadataID           = metadataId,
                RequestPartyName     = "AVA.Vault"
            });
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
