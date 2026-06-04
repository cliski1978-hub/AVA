using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using AVA.Memory.Abstractions;
using AVA.UI.CORE.Models.UI;
using AVA.UI.Vault.Mapping;
using AVA.Vault.Core.Adapters;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Files;
using AVA.Vault.Core.Dtos.Navigation;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Dtos.Workflows;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Data;
using AVA.Vault.Core.Services.Interfaces;

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

    private readonly IVaultNavigationReadService _navRead;
    private readonly IVaultWorkflowDetailsReadService _wfDetails;
    private readonly IVaultWorkflowGraphReadService _wfGraph;
    private readonly IVaultNoteDetailsReadService _noteDetails;
    private readonly IVaultNoteUsageReadService _noteUsage;
    private readonly IVaultFileDetailsReadService _fileDetails;
    private readonly IVaultFileUsageReadService _fileUsage;
    private readonly IVaultContextFilesReadService _ctxFiles;

    public VaultUiSyncService(
        IDbContextFactory<VaultDbContext> dbFactory,
        IVaultPersistenceProvider dbProvider,
        IMemoryStore memoryStore,
        VaultLogger vaultLogger,
        ILogger<VaultUiSyncService> logger,
        IVaultNavigationReadService navRead,
        IVaultWorkflowDetailsReadService wfDetails,
        IVaultWorkflowGraphReadService wfGraph,
        IVaultNoteDetailsReadService noteDetails,
        IVaultNoteUsageReadService noteUsage,
        IVaultFileDetailsReadService fileDetails,
        IVaultFileUsageReadService fileUsage,
        IVaultContextFilesReadService ctxFiles)
    {
        _dbFactory = dbFactory;
        _dbProvider = dbProvider;
        _memoryStore = memoryStore;
        _vaultLogger = vaultLogger;
        _logger = logger;
        _navRead = navRead;
        _wfDetails = wfDetails;
        _wfGraph = wfGraph;
        _noteDetails = noteDetails;
        _noteUsage = noteUsage;
        _fileDetails = fileDetails;
        _fileUsage = fileUsage;
        _ctxFiles = ctxFiles;
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
                    VaultId = header.ID,
                    Name = header.DisplayName,
                    IsExpanded = true
                };

                var projectIndex = new Dictionary<string, ProjectState>();
                foreach (var p in projects)
                {
                    var ps = new ProjectState
                    {
                        ProjectId = p.ID,
                        Name = p.Name,
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

    private static SessionState MapToSessionState(AVA.Vault.Core.Data.Models.VaultSession s) => new()
    {
        SessionId    = s.ID,
        Name         = s.Name,
        CreatedAt    = s.CreatedAt,
        LastActiveAt = s.LastActiveAt,
        IsPinned     = s.IsPinned
    };

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
        => _dbProvider.CreateVaultNoteAsync(vaultId, projectId, title, content, sessionId);

    public Task<VaultNote?> GetNoteAsync(string vaultId, string noteId)
        => _dbProvider.GetVaultNoteAsync(vaultId, noteId);

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
        string? projectId = null,
        string? sessionId = null,
        string? keyword = null,
        string? tag = null,
        string sortBy = "Updated",
        bool sortDescending = true,
        DateTime? createdAfter = null,
        DateTime? createdBefore = null,
        DateTime? updatedAfter = null,
        DateTime? updatedBefore = null,
        CancellationToken ct = default)
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

    #region New

    // ── Workflow ──────────────────────────────────────────────────────────────

    public Task<CreateVaultWorkflowResponse> CreateWorkflowAsync(string vaultId, string? projectId, string name)
        => _dbProvider.CreateWorkflowAsync(vaultId, projectId, name);

    public Task<UpdateVaultWorkflowResponse> UpdateWorkflowAsync(string workflowId, string? name, string? description = null, string? status = null, int? sortOrder = null)
        => _dbProvider.UpdateWorkflowAsync(workflowId, name, description, status, sortOrder);

    public Task<DeleteVaultWorkflowResponse> DeleteWorkflowAsync(string workflowId)
        => _dbProvider.DeleteWorkflowAsync(workflowId);

    // ── Workflow Node ─────────────────────────────────────────────────────────

    public Task<CreateVaultWorkflowNodeResponse> CreateWorkflowNodeAsync(string workflowId, string name, string? workflowNodeId = null, string? description = null, string? instructions = null, string? metadataJson = null, string? nodeType = null, int? nodeOrder = null, string? status = null)
        => _dbProvider.CreateWorkflowNodeAsync(workflowId, name, workflowNodeId, description, instructions, metadataJson, nodeType, nodeOrder, status);

    public Task<UpdateVaultWorkflowNodeResponse> UpdateWorkflowNodeAsync(string workflowNodeId, string? name = null, string? description = null, string? instructions = null, string? nodeType = null, int? nodeOrder = null, string? status = null)
        => _dbProvider.UpdateWorkflowNodeAsync(workflowNodeId, name, description, instructions, nodeType, nodeOrder, status);

    public Task<DeleteVaultWorkflowNodeResponse> DeleteWorkflowNodeAsync(string workflowNodeId)
        => _dbProvider.DeleteWorkflowNodeAsync(workflowNodeId);

    // ── Workflow Line ─────────────────────────────────────────────────────────

    public Task<CreateVaultWorkflowLineResponse> CreateWorkflowLineAsync(string workflowId, string sourceWorkflowNodeId, string targetWorkflowNodeId, string name, string? workflowLineId = null, string? description = null, string? conditionJson = null, bool? isDefaultLine = null, string? lineType = null, int? lineOrder = null)
        => _dbProvider.CreateWorkflowLineAsync(workflowId, sourceWorkflowNodeId, targetWorkflowNodeId, name, workflowLineId, description, conditionJson, isDefaultLine, lineType, lineOrder);

    public Task<UpdateVaultWorkflowLineResponse> UpdateWorkflowLineAsync(string workflowLineId, string? name = null, string? description = null, string? conditionJson = null, bool? isDefaultLine = null, string? lineType = null, int? lineOrder = null)
        => _dbProvider.UpdateWorkflowLineAsync(workflowLineId, name, description, conditionJson, isDefaultLine, lineType, lineOrder);

    public Task<DeleteVaultWorkflowLineResponse> DeleteWorkflowLineAsync(string workflowLineId)
        => _dbProvider.DeleteWorkflowLineAsync(workflowLineId);

    // ── Workflow Line Step ────────────────────────────────────────────────────

    public Task<CreateVaultWorkflowLineStepResponse> CreateWorkflowLineStepAsync(string workflowLineId, string name, string? workflowLineStepId = null, string? description = null, string? instructions = null, bool? isRequired = null, int? stepOrder = null, string? stepType = null)
        => _dbProvider.CreateWorkflowLineStepAsync(workflowLineId, name, workflowLineStepId, description, instructions, isRequired, stepOrder, stepType);

    public Task<UpdateVaultWorkflowLineStepResponse> UpdateWorkflowLineStepAsync(string workflowLineStepId, string? name = null, string? description = null, string? instructions = null, bool? isRequired = null, int? stepOrder = null, string? stepType = null)
        => _dbProvider.UpdateWorkflowLineStepAsync(workflowLineStepId, name, description, instructions, isRequired, stepOrder, stepType);

    public Task<DeleteVaultWorkflowLineStepResponse> DeleteWorkflowLineStepAsync(string workflowLineStepId)
        => _dbProvider.DeleteWorkflowLineStepAsync(workflowLineStepId);

    // ── Workflow Note Links ───────────────────────────────────────────────────

    public Task<CreateVaultWorkflowNoteResponse> CreateWorkflowNoteAsync(string workflowId, string noteId, string? workflowNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateWorkflowNoteAsync(workflowId, noteId, workflowNoteId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultWorkflowNoteResponse> UpdateWorkflowNoteAsync(string workflowNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateWorkflowNoteAsync(workflowNoteId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultWorkflowNoteResponse> DeleteWorkflowNoteAsync(string workflowNoteId)
        => _dbProvider.DeleteWorkflowNoteAsync(workflowNoteId);

    public Task<CreateVaultWorkflowNodeNoteResponse> CreateWorkflowNodeNoteAsync(string workflowNodeId, string noteId, string? workflowNodeNoteId = null, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null)
        => _dbProvider.CreateWorkflowNodeNoteAsync(workflowNodeId, noteId, workflowNodeNoteId, instructions, isRequired, noteOrder, usageRole);

    public Task<UpdateVaultWorkflowNodeNoteResponse> UpdateWorkflowNodeNoteAsync(string workflowNodeNoteId, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null)
        => _dbProvider.UpdateWorkflowNodeNoteAsync(workflowNodeNoteId, usageRole, instructions, isRequired, noteOrder);

    public Task<DeleteVaultWorkflowNodeNoteResponse> DeleteWorkflowNodeNoteAsync(string workflowNodeNoteId)
        => _dbProvider.DeleteWorkflowNodeNoteAsync(workflowNodeNoteId);

    public Task<CreateVaultWorkflowLineNoteResponse> CreateWorkflowLineNoteAsync(string workflowLineId, string noteId, string? workflowLineNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateWorkflowLineNoteAsync(workflowLineId, noteId, workflowLineNoteId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultWorkflowLineNoteResponse> UpdateWorkflowLineNoteAsync(string workflowLineNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateWorkflowLineNoteAsync(workflowLineNoteId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultWorkflowLineNoteResponse> DeleteWorkflowLineNoteAsync(string workflowLineNoteId)
        => _dbProvider.DeleteWorkflowLineNoteAsync(workflowLineNoteId);

    public Task<CreateVaultWorkflowLineStepNoteResponse> CreateWorkflowLineStepNoteAsync(string workflowLineStepId, string noteId, string? workflowLineStepNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateWorkflowLineStepNoteAsync(workflowLineStepId, noteId, workflowLineStepNoteId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultWorkflowLineStepNoteResponse> UpdateWorkflowLineStepNoteAsync(string workflowLineStepNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateWorkflowLineStepNoteAsync(workflowLineStepNoteId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultWorkflowLineStepNoteResponse> DeleteWorkflowLineStepNoteAsync(string workflowLineStepNoteId)
        => _dbProvider.DeleteWorkflowLineStepNoteAsync(workflowLineStepNoteId);

    // ── Workflow FileRef Links ────────────────────────────────────────────────

    public Task<CreateVaultWorkflowFileRefResponse> CreateWorkflowFileRefAsync(string workflowId, string fileRefId, string? workflowFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateWorkflowFileRefAsync(workflowId, fileRefId, workflowFileRefId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultWorkflowFileRefResponse> UpdateWorkflowFileRefAsync(string workflowFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateWorkflowFileRefAsync(workflowFileRefId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultWorkflowFileRefResponse> DeleteWorkflowFileRefAsync(string workflowFileRefId)
        => _dbProvider.DeleteWorkflowFileRefAsync(workflowFileRefId);

    public Task<CreateVaultWorkflowNodeFileRefResponse> CreateWorkflowNodeFileRefAsync(string workflowNodeId, string fileRefId, string? workflowNodeFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateWorkflowNodeFileRefAsync(workflowNodeId, fileRefId, workflowNodeFileRefId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultWorkflowNodeFileRefResponse> UpdateWorkflowNodeFileRefAsync(string workflowNodeFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateWorkflowNodeFileRefAsync(workflowNodeFileRefId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultWorkflowNodeFileRefResponse> DeleteWorkflowNodeFileRefAsync(string workflowNodeFileRefId)
        => _dbProvider.DeleteWorkflowNodeFileRefAsync(workflowNodeFileRefId);

    public Task<CreateVaultWorkflowLineFileRefResponse> CreateWorkflowLineFileRefAsync(string workflowLineId, string fileRefId, string? workflowLineFileRefId = null, string? instructions = null, bool? isRequired = null, int? fileOrder = null, string? usageRole = null)
        => _dbProvider.CreateWorkflowLineFileRefAsync(workflowLineId, fileRefId, workflowLineFileRefId, instructions, isRequired, fileOrder, usageRole);

    public Task<UpdateVaultWorkflowLineFileRefResponse> UpdateWorkflowLineFileRefAsync(string workflowLineFileRefId, string? instructions = null, bool? isRequired = null, int? fileOrder = null, string? usageRole = null)
        => _dbProvider.UpdateWorkflowLineFileRefAsync(workflowLineFileRefId, usageRole, instructions, isRequired, fileOrder);

    public Task<DeleteVaultWorkflowLineFileRefResponse> DeleteWorkflowLineFileRefAsync(string workflowLineFileRefId)
        => _dbProvider.DeleteWorkflowLineFileRefAsync(workflowLineFileRefId);

    public Task<CreateVaultWorkflowLineStepFileRefResponse> CreateWorkflowLineStepFileRefAsync(string workflowLineStepId, string fileRefId, string? workflowLineStepFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateWorkflowLineStepFileRefAsync(workflowLineStepId, fileRefId, workflowLineStepFileRefId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultWorkflowLineStepFileRefResponse> UpdateWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateWorkflowLineStepFileRefAsync(workflowLineStepFileRefId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultWorkflowLineStepFileRefResponse> DeleteWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId)
        => _dbProvider.DeleteWorkflowLineStepFileRefAsync(workflowLineStepFileRefId);

    // ── Vault / Project / Session Note Links ─────────────────────────────────

    public Task<CreateVaultHeaderNoteResponse> CreateVaultHeaderNoteAsync(string vaultId, string noteId, string? headerNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateVaultHeaderNoteAsync(vaultId, noteId, headerNoteId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultHeaderNoteResponse> UpdateVaultHeaderNoteAsync(string headerNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateVaultHeaderNoteAsync(headerNoteId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultHeaderNoteResponse> DeleteVaultHeaderNoteAsync(string headerNoteId)
        => _dbProvider.DeleteVaultHeaderNoteAsync(headerNoteId);

    public Task<CreateVaultProjectNoteResponse> CreateVaultProjectNoteAsync(string projectId, string noteId, string? projectNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateVaultProjectNoteAsync(projectId, noteId, projectNoteId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultProjectNoteResponse> UpdateVaultProjectNoteAsync(string projectNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateVaultProjectNoteAsync(projectNoteId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultProjectNoteResponse> DeleteVaultProjectNoteAsync(string projectNoteId)
        => _dbProvider.DeleteVaultProjectNoteAsync(projectNoteId);

    public Task<CreateVaultSessionNoteResponse> CreateVaultSessionNoteAsync(string sessionId, string noteId, string? sessionNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateVaultSessionNoteAsync(sessionId, noteId, sessionNoteId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultSessionNoteResponse> UpdateVaultSessionNoteAsync(string sessionNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateVaultSessionNoteAsync(sessionNoteId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultSessionNoteResponse> DeleteVaultSessionNoteAsync(string sessionNoteId)
        => _dbProvider.DeleteVaultSessionNoteAsync(sessionNoteId);

    // ── Vault / Project / Session FileRef Links ──────────────────────────────

    public Task<CreateVaultHeaderFileRefResponse> CreateVaultHeaderFileRefAsync(string vaultId, string fileRefId, string? headerFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateVaultHeaderFileRefAsync(vaultId, fileRefId, headerFileRefId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultHeaderFileRefResponse> UpdateVaultHeaderFileRefAsync(string headerFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateVaultHeaderFileRefAsync(headerFileRefId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultHeaderFileRefResponse> DeleteVaultHeaderFileRefAsync(string headerFileRefId)
        => _dbProvider.DeleteVaultHeaderFileRefAsync(headerFileRefId);

    public Task<CreateVaultProjectFileRefResponse> CreateVaultProjectFileRefAsync(string projectId, string fileRefId, string? projectFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateVaultProjectFileRefAsync(projectId, fileRefId, projectFileRefId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultProjectFileRefResponse> UpdateVaultProjectFileRefAsync(string projectFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateVaultProjectFileRefAsync(projectFileRefId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultProjectFileRefResponse> DeleteVaultProjectFileRefAsync(string projectFileRefId)
        => _dbProvider.DeleteVaultProjectFileRefAsync(projectFileRefId);

    public Task<CreateVaultSessionFileRefResponse> CreateVaultSessionFileRefAsync(string sessionId, string fileRefId, string? sessionFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateVaultSessionFileRefAsync(sessionId, fileRefId, sessionFileRefId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultSessionFileRefResponse> UpdateVaultSessionFileRefAsync(string sessionFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateVaultSessionFileRefAsync(sessionFileRefId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultSessionFileRefResponse> DeleteVaultSessionFileRefAsync(string sessionFileRefId)
        => _dbProvider.DeleteVaultSessionFileRefAsync(sessionFileRefId);

    // ── FileRefs ──────────────────────────────────────────────────────────────

    public Task<CreateVaultFileRefResponse> CreateFileRefAsync(string vaultId, string name, string path, string? fileRefId = null, string? projectId = null, string? sessionId = null, long? fileSizeBytes = null, string? contentHash = null, string? mimeType = null, int? fileOrder = null)
        => _dbProvider.CreateFileRefAsync(vaultId, name, path, fileRefId, projectId, sessionId, fileSizeBytes, contentHash, mimeType, fileOrder);

    public Task<UpdateVaultFileRefResponse> UpdateFileRefAsync(string fileRefId, string? name = null, string? path = null, string? mimeType = null, string? contentHash = null, long? fileSizeBytes = null, int? fileOrder = null, string? vaultId = null, string? projectId = null, string? sessionId = null)
        => _dbProvider.UpdateFileRefAsync(fileRefId, name, path, mimeType, contentHash, fileSizeBytes, fileOrder, vaultId, projectId, sessionId);

    public Task<DeleteVaultFileRefResponse> DeleteFileRefAsync(string fileRefId)
        => _dbProvider.DeleteFileRefAsync(fileRefId);

    // ── FileRef Note Links ────────────────────────────────────────────────────

    public Task<CreateVaultFileRefNoteResponse> CreateFileRefNoteAsync(string fileRefId, string noteId, string? fileRefNoteId = null, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null)
        => _dbProvider.CreateFileRefNoteAsync(fileRefId, noteId, fileRefNoteId, instructions, isRequired, noteOrder, usageRole);

    public Task<UpdateVaultFileRefNoteResponse> UpdateFileRefNoteAsync(string fileRefNoteId, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null)
        => _dbProvider.UpdateFileRefNoteAsync(fileRefNoteId, usageRole, instructions, isRequired, noteOrder);

    public Task<DeleteVaultFileRefNoteResponse> DeleteFileRefNoteAsync(string fileRefNoteId)
        => _dbProvider.DeleteFileRefNoteAsync(fileRefNoteId);

    // ── Note FileRef Links ────────────────────────────────────────────────────

    public Task<CreateVaultNoteFileRefResponse> CreateNoteFileRefAsync(string noteId, string fileRefId, string? noteFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.CreateNoteFileRefAsync(noteId, fileRefId, noteFileRefId, instructions, isRequired, sortOrder, usageRole);

    public Task<UpdateVaultNoteFileRefResponse> UpdateNoteFileRefAsync(string noteFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null)
        => _dbProvider.UpdateNoteFileRefAsync(noteFileRefId, usageRole, instructions, isRequired, sortOrder);

    public Task<DeleteVaultNoteFileRefResponse> DeleteNoteFileRefAsync(string noteFileRefId)
        => _dbProvider.DeleteNoteFileRefAsync(noteFileRefId);

    // ── FileRef Relations ─────────────────────────────────────────────────────

    public Task<CreateVaultFileRefRelationResponse> CreateFileRefRelationAsync(string sourceFileRefId, string targetFileRefId, string relationType, string? description = null, string? fileRefRelationId = null, int? sortOrder = null, float? weight = null)
        => _dbProvider.CreateFileRefRelationAsync(sourceFileRefId, targetFileRefId, relationType, description, fileRefRelationId, sortOrder, weight);

    public Task<UpdateVaultFileRefRelationResponse> UpdateFileRefRelationAsync(string fileRefRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null)
        => _dbProvider.UpdateFileRefRelationAsync(fileRefRelationId, relationType: relationType, description: description, sortOrder: sortOrder, weight: weight);

    public Task<DeleteVaultFileRefRelationResponse> DeleteFileRefRelationAsync(string fileRefRelationId)
        => _dbProvider.DeleteFileRefRelationAsync(fileRefRelationId);

    // ── Note Relations / Metadata / Tags ──────────────────────────────────────

    public Task<CreateVaultNoteRelationResponse> CreateNoteRelationAsync(string sourceNoteId, string targetNoteId, string relationType, string? description = null, string? noteRelationId = null, int? sortOrder = null, float? weight = null)
        => _dbProvider.CreateNoteRelationAsync(sourceNoteId, targetNoteId, relationType, description, noteRelationId, sortOrder, weight);

    public Task<UpdateVaultNoteRelationResponse> UpdateNoteRelationAsync(string noteRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null)
        => _dbProvider.UpdateNoteRelationAsync(noteRelationId, relationType: relationType, description: description, sortOrder: sortOrder, weight: weight);

    public Task<DeleteVaultNoteRelationResponse> DeleteNoteRelationAsync(string noteRelationId)
        => _dbProvider.DeleteNoteRelationAsync(noteRelationId);

    public Task<CreateVaultNoteVaultTagResponse> CreateNoteVaultTagAsync(string noteId, string tagId, string? noteVaultTagId = null, int? sortOrder = null)
        => _dbProvider.CreateNoteVaultTagAsync(noteId, tagId, noteVaultTagId, sortOrder);

    public Task<UpdateVaultNoteVaultTagResponse> UpdateNoteVaultTagAsync(string noteVaultTagId, int? sortOrder = null)
        => _dbProvider.UpdateNoteVaultTagAsync(noteVaultTagId, sortOrder: sortOrder);

    public Task<DeleteVaultNoteVaultTagResponse> DeleteNoteVaultTagAsync(string noteVaultTagId)
        => _dbProvider.DeleteNoteVaultTagAsync(noteVaultTagId);

    public Task<UpdateVaultTagResponse> UpdateTagAsync(string tagId, string? name = null, string? description = null, string? colorHex = null, int? sortOrder = null, string? vaultId = null)
        => _dbProvider.UpdateTagAsync(tagId, name, description, colorHex, sortOrder, vaultId);

    public Task<CreateVaultMetadataResponse> CreateMetadataAsync(string noteId, string key, string? value = null, string? metadataId = null)
        => _dbProvider.CreateMetadataAsync(noteId, key, value, metadataId);

    public Task<UpdateVaultMetadataResponse> UpdateMetadataAsync(string metadataId, string? key = null, string? value = null)
        => _dbProvider.UpdateMetadataAsync(metadataId, key: key, value: value);

    public Task<DeleteVaultMetadataResponse> DeleteMetadataAsync(string metadataId)
        => _dbProvider.DeleteMetadataAsync(metadataId);

    #endregion

    // ── Read Queries ──────────────────────────────────────────────────────────

    public Task<VaultNavigationTreeDto> GetVaultNavigationTreeAsync(string vaultId, CancellationToken ct = default)
        => _navRead.GetVaultNavigationTreeAsync(vaultId, ct);

    public Task<VaultNavigationTreeDto> GetAllVaultNavigationTreesAsync(CancellationToken ct = default)
        => _navRead.GetAllVaultNavigationTreesAsync(ct);

    public Task<VaultNavigationProjectDto> GetProjectNavigationBranchAsync(string projectId, CancellationToken ct = default)
        => _navRead.GetProjectNavigationBranchAsync(projectId, ct);

    public Task<VaultWorkflowDetailsDto?> GetWorkflowDetailsAsync(string workflowId, CancellationToken ct = default)
        => _wfDetails.GetWorkflowDetailsAsync(workflowId, ct);

    public Task<VaultWorkflowGraphDto?> GetWorkflowGraphAsync(string workflowId, CancellationToken ct = default)
        => _wfGraph.GetWorkflowGraphAsync(workflowId, ct);

    public Task<VaultNoteDetailsDto?> GetNoteDetailsAsync(string noteId, CancellationToken ct = default)
        => _noteDetails.GetNoteDetailsAsync(noteId, ct);

    public Task<VaultNoteUsageDto> GetNoteUsageAsync(string noteId, CancellationToken ct = default)
        => _noteUsage.GetNoteUsageAsync(noteId, ct);

    public Task<VaultFileDetailsDto?> GetFileDetailsAsync(string fileRefId, CancellationToken ct = default)
        => _fileDetails.GetFileDetailsAsync(fileRefId, ct);

    public Task<VaultFileUsageDto> GetFileUsageAsync(string fileRefId, CancellationToken ct = default)
        => _fileUsage.GetFileUsageAsync(fileRefId, ct);

    public Task<VaultContextFilesDto> GetFilesForVaultAsync(string vaultId, CancellationToken ct = default)
        => _ctxFiles.GetFilesForVaultAsync(vaultId, ct);

    public Task<VaultContextFilesDto> GetFilesForProjectAsync(string projectId, CancellationToken ct = default)
        => _ctxFiles.GetFilesForProjectAsync(projectId, ct);

    public Task<VaultContextFilesDto> GetFilesForSessionAsync(string sessionId, CancellationToken ct = default)
        => _ctxFiles.GetFilesForSessionAsync(sessionId, ct);

    public Task<VaultContextFilesDto> GetFilesForNoteAsync(string noteId, CancellationToken ct = default)
        => _ctxFiles.GetFilesForNoteAsync(noteId, ct);

    public Task<VaultContextFilesDto> GetFilesForWorkflowAsync(string workflowId, CancellationToken ct = default)
        => _ctxFiles.GetFilesForWorkflowAsync(workflowId, ct);
}
