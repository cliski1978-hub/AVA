using Microsoft.EntityFrameworkCore;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Data;


namespace AVA.Vault.Core.Adapters
{
    /// <summary>
    /// IVaultPersistenceProvider implementation for SQL Server storage.
    /// Routes all write operations through Vault Core API services and returns
    /// the CfkApiResponse-derived response object directly — no exceptions thrown,
    /// no wrapping. Succeeded and UserMessage travel intact to the calling ViewModel.
    /// </summary>
    public class DbVaultPersistenceProvider : IVaultPersistenceProvider
    {
        private readonly IDbContextFactory<VaultDbContext> _dbFactory;
        private readonly VaultLogger _logger;
        private readonly IVaultIdService _ids;

        public DbVaultPersistenceProvider(IDbContextFactory<VaultDbContext> dbFactory, VaultLogger logger, IVaultIdService ids)
        {
            _dbFactory = dbFactory;
            _logger    = logger;
            _ids       = ids;
        }

        // ── Vault ──────────────────────────────────────────────────────────────
        #region Vault Core
        public async Task<CreateVaultHeaderResponse> CreateVaultAsync(string name, string? vaultId = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new CreateVaultHeaderService(adapter, _logger)
                .Execute(new CreateVaultHeaderRequest
                {
                    VaultId          = vaultId,
                    DisplayName      = name,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultHeaderResponse> RenameVaultAsync(string vaultId, string newName, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new UpdateVaultHeaderService(adapter, _logger)
                .Execute(new UpdateVaultHeaderRequest
                {
                    VaultId          = vaultId,
                    DisplayName      = newName,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultHeaderResponse> DeleteVaultAsync(string vaultId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new DeleteVaultHeaderService(adapter, _logger)
                .Execute(new DeleteVaultHeaderRequest
                {
                    VaultId          = vaultId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<IEnumerable<VaultHeader>> ListVaultsAsync(CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.VaultHeaders.Where(h => h.IsActive).OrderBy(h => h.CreatedAt).ToListAsync(ct);
        }

        // ── Project ────────────────────────────────────────────────────────────

        public async Task<CreateVaultProjectResponse> CreateProjectAsync(string vaultId, string name, string? projectId = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new CreateVaultProjectService(adapter, _logger)
                .Execute(new CreateVaultProjectRequest
                {
                    ProjectID        = projectId,
                    VaultID          = vaultId,
                    Name             = name,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultProjectResponse> RenameProjectAsync(string vaultId, string projectId, string newName, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new UpdateVaultProjectService(adapter, _logger)
                .Execute(new UpdateVaultProjectRequest
                {
                    VaultID          = vaultId,
                    ProjectID        = projectId,
                    Name             = newName,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultProjectResponse> DeleteProjectAsync(string vaultId, string projectId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new DeleteVaultProjectService(adapter, _logger)
                .Execute(new DeleteVaultProjectRequest
                {
                    VaultID          = vaultId,
                    ProjectID        = projectId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<IEnumerable<VaultProject>> ListProjectsAsync(string vaultId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.VaultProjects.Where(p => p.VaultID == vaultId).OrderBy(p => p.CreatedAt).ToListAsync(ct);
        }

        // ── Session ────────────────────────────────────────────────────────────

        public async Task<CreateVaultSessionResponse> CreateSessionAsync(string vaultId, string? projectId, string name, string? sessionId = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new CreateVaultSessionService(adapter, _logger)
                .Execute(new CreateVaultSessionRequest
                {
                    SessionID        = sessionId,
                    VaultID          = vaultId,
                    ProjectID        = projectId,
                    Name             = name,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultSessionResponse> RenameSessionAsync(string vaultId, string sessionId, string newName, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new UpdateVaultSessionService(adapter, _logger)
                .Execute(new UpdateVaultSessionRequest
                {
                    VaultID          = vaultId,
                    SessionID        = sessionId,
                    Name             = newName,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultSessionResponse> UpdateSessionModelStateAsync(string vaultId, string sessionId, List<string> attachedModelIds, List<string> broadcastGroupIds, string? defaultModelId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new UpdateVaultSessionService(adapter, _logger)
                .Execute(new UpdateVaultSessionRequest
                {
                    VaultID               = vaultId,
                    SessionID             = sessionId,
                    AttachedModelIdsJson  = System.Text.Json.JsonSerializer.Serialize(attachedModelIds),
                    BroadcastGroupIdsJson = System.Text.Json.JsonSerializer.Serialize(broadcastGroupIds),
                    DefaultModelId        = defaultModelId,
                    RequestPartyName      = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultSessionResponse> DeleteSessionAsync(string vaultId, string sessionId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new DeleteVaultSessionService(adapter, _logger)
                .Execute(new DeleteVaultSessionRequest
                {
                    VaultID          = vaultId,
                    SessionID        = sessionId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<IEnumerable<VaultSession>> ListSessionsAsync(string vaultId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.VaultSessions.Where(s => s.VaultID == vaultId).OrderBy(s => s.CreatedAt).ToListAsync(ct);
        }

        // ── Note ───────────────────────────────────────────────────────────────

        public async Task<CreateVaultNoteResponse> CreateVaultNoteAsync(string vaultId, string? projectId, string title, string content, string? sessionId = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new CreateVaultNoteService(adapter, _logger)
                .Execute(new CreateVaultNoteRequest
                {
                    VaultID          = vaultId,
                    ProjectID        = projectId,
                    SessionID        = sessionId,
                    Title            = title,
                    Content          = content,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<VaultNote?> GetVaultNoteAsync(string vaultId, string noteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            var response       = new GetVaultNoteService(adapter, _logger)
                .Execute(new GetVaultNoteRequest
                {
                    VaultID          = vaultId,
                    NoteID           = noteId,
                    RequestPartyName = "AVA.Vault"
                });
            return response.Note;
        }

        public async Task<UpdateVaultNoteResponse> UpdateNoteAsync(string vaultId,string noteId, string? title, string? content, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new UpdateVaultNoteService(adapter, _logger)
                .Execute(new UpdateVaultNoteRequest
                {
                   
                    NoteID           = noteId,
                    Title            = title,
                    Content          = content,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultNoteResponse> DeleteNoteAsync(string vaultId, string noteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new DeleteVaultNoteService(adapter, _logger)
                .Execute(new DeleteVaultNoteRequest
                {
                    NoteID           = noteId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<IEnumerable<VaultNote>> SearchNotesAsync(string vaultId, string? projectId = null, string? sessionId = null, string? keyword = null, string? tag = null, string sortBy = "Updated", bool sortDescending = true, DateTime? createdAfter = null, DateTime? createdBefore = null, DateTime? updatedAfter = null, DateTime? updatedBefore = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            var response       = new SearchVaultNotesService(adapter, _logger)
                .Execute(new SearchVaultNotesRequest
                {
                    VaultID          = vaultId,
                    ProjectID        = projectId,
                    SessionID        = sessionId,
                    Keyword          = keyword,
                    Tag              = tag,
                    SortBy           = sortBy,
                    SortDescending   = sortDescending,
                    CreatedAfter     = createdAfter,
                    CreatedBefore    = createdBefore,
                    UpdatedAfter     = updatedAfter,
                    UpdatedBefore    = updatedBefore,
                    RequestPartyName = "AVA.Vault"
                });
            return response.Notes;
        }

        // ── Tag ───────────────────────────────────────────────────────────────

        public async Task<CreateVaultTagResponse> CreateTagAsync(string vaultId, string name, string? color = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new CreateVaultTagService(adapter, _logger, _ids)
                .Execute(new CreateVaultTagRequest
                {
                    VaultID          = vaultId,
                    Name             = name,
                    Color            = color,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultTagResponse> RenameTagAsync(string vaultId, string tagId, string newName, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new UpdateVaultTagService(adapter, _logger)
                .Execute(new UpdateVaultTagRequest
                {
                    VaultID          = vaultId,
                    TagID            = tagId,
                    Name             = newName,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultTagResponse> DeleteTagAsync(string vaultId, string tagId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new DeleteVaultTagService(adapter, _logger)
                .Execute(new DeleteVaultTagRequest
                {
                    VaultID          = vaultId,
                    TagID            = tagId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<IEnumerable<VaultTag>> ListTagsAsync(string vaultId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.VaultTags.Where(t => t.ProjectID == vaultId).OrderBy(t => t.Name).ToListAsync(ct);
        }

        public async Task<AssignTagToNoteResponse> AssignTagToNoteAsync(string vaultId, string noteId, string tagId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new AssignTagToNoteService(adapter, _logger)
                .Execute(new AssignTagToNoteRequest
                {
                    VaultID          = vaultId,
                    NoteID           = noteId,
                    TagID            = tagId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<RemoveTagFromNoteResponse> RemoveTagFromNoteAsync(string vaultId, string noteId, string tagId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new RemoveTagFromNoteService(adapter, _logger)
                .Execute(new RemoveTagFromNoteRequest
                {
                    VaultID          = vaultId,
                    NoteID           = noteId,
                    TagID            = tagId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        // ── Link ───────────────────────────────────────────────────────────────

        public async Task<CreateVaultRelationResponse> CreateRelationAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string? description = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new CreateVaultRelationService(adapter, _logger, _ids)
                .Execute(new CreateVaultRelationRequest
                {
                    VaultID          = vaultId,
                    SourceNoteID     = sourceNoteId,
                    TargetNoteID     = targetNoteId,
                    RelationType     = relationType,
                    Description      = description,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultLinkResponse> DeleteRelationAsync(string vaultId, string linkId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new DeleteVaultLinkService(adapter, _logger)
                .Execute(new DeleteVaultLinkRequest
                {
                    VaultID          = vaultId,
                    LinkID           = linkId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<IEnumerable<RelatedNoteResult>> GetRelatedNotesAsync(string vaultId, string noteId, string? relationType = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            var response       = new GetRelatedVaultNotesService(adapter, _logger)
                .Execute(new GetRelatedVaultNotesRequest
                {
                    VaultID          = vaultId,
                    NoteID           = noteId,
                    RelationType     = relationType,
                    RequestPartyName = "AVA.Vault"
                });
            return response.Related;
        }

        #endregion

        #region Workflows

        public async Task<CreateVaultWorkflowResponse> CreateWorkflowAsync(string vaultId, string? projectId, string name, string? description = null, string? workflowType = null, string? status = null, int? sortOrder = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowService(adapter, _logger)
                .Execute(new CreateVaultWorkflowRequest
                {
                    VaultID = vaultId,
                    ProjectID = projectId,
                    Name = name,
                    Description = description,
                    WorkflowType = workflowType,
                    Status = status,
                    SortOrder = sortOrder ?? 0,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowResponse> UpdateWorkflowAsync(string workflowId, string? name, string? description, string? status, int? sortOrder = null, string? projectId = null, string? workflowType = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowRequest
                {
                    WorkflowID = workflowId,
                    Name = name,
                    Description = description,
                    Status = status,
                    SortOrder = sortOrder,
                    ProjectID = projectId,
                    WorkflowType = workflowType,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowResponse> DeleteWorkflowAsync(string workflowId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowRequest
                {
                    WorkflowID = workflowId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region Workflow Nodes

        public async Task<CreateVaultWorkflowNodeResponse> CreateWorkflowNodeAsync(string workflowId, string name, string? workflowNodeId = null, string? description = null, string? instructions = null, string? metadataJson = null, string? nodeType = null, int? nodeOrder = null, string? status = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowNodeService(adapter, _logger)
                .Execute(new CreateVaultWorkflowNodeRequest
                {
                    WorkflowID = workflowId,
                    Name = name,
                    WorkflowNodeID = workflowNodeId,
                    Description = description,
                    Instructions = instructions,
                    MetadataJson = metadataJson,
                    NodeType = nodeType,
                    NodeOrder = nodeOrder ?? 0,
                    Status = status,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowNodeResponse> UpdateWorkflowNodeAsync(string workflowNodeId, string? name = null, string? description = null, string? instructions = null, string? nodeType = null, int? nodeOrder = null, string? status = null, string? metadataJson = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowNodeService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowNodeRequest
                {
                    WorkflowNodeID = workflowNodeId,
                    Name = name,
                    Description = description,
                    Instructions = instructions,
                    NodeType = nodeType,
                    NodeOrder = nodeOrder,
                    Status = status,
                    MetadataJson = metadataJson,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowNodeResponse> DeleteWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowNodeService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowNodeRequest
                {
                    WorkflowNodeID = workflowNodeId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region Workflow Lines

        public async Task<CreateVaultWorkflowLineResponse> CreateWorkflowLineAsync(string workflowId, string sourceWorkflowNodeId, string targetWorkflowNodeId, string name, string? workflowLineId = null, string? description = null, string? conditionJson = null, bool? isDefaultLine = null, string? lineType = null, int? lineOrder = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowLineService(adapter, _logger)
                .Execute(new CreateVaultWorkflowLineRequest
                {
                    WorkflowID = workflowId,
                    SourceWorkflowNodeID = sourceWorkflowNodeId,
                    TargetWorkflowNodeID = targetWorkflowNodeId,
                    Name = name,
                    WorkflowLineID = workflowLineId,
                    Description = description,
                    ConditionJson = conditionJson,
                    IsDefaultLine = isDefaultLine ?? false,
                    LineType = lineType,
                    LineOrder = lineOrder ?? 0,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowLineResponse> UpdateWorkflowLineAsync(string workflowLineId, string? name = null, string? description = null, string? conditionJson = null, bool? isDefaultLine = null, string? lineType = null, int? lineOrder = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowLineService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowLineRequest
                {
                    WorkflowLineID = workflowLineId,
                    Name = name,
                    Description = description,
                    ConditionJson = conditionJson,
                    IsDefaultLine = isDefaultLine,
                    LineType = lineType,
                    LineOrder = lineOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowLineResponse> DeleteWorkflowLineAsync(string workflowLineId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowLineService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowLineRequest
                {
                    WorkflowLineID = workflowLineId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region Workflow Line Steps

        public async Task<CreateVaultWorkflowLineStepResponse> CreateWorkflowLineStepAsync(string workflowLineId, string name, string? workflowLineStepId = null, string? description = null, string? instructions = null, bool? isRequired = null, int? stepOrder = null, string? stepType = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowLineStepService(adapter, _logger)
                .Execute(new CreateVaultWorkflowLineStepRequest
                {
                    WorkflowLineID = workflowLineId,
                    Name = name,
                    WorkflowLineStepID = workflowLineStepId,
                    Description = description,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    StepOrder = stepOrder ?? 0,
                    StepType = stepType,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowLineStepResponse> UpdateWorkflowLineStepAsync(string workflowLineStepId, string? name = null, string? description = null, string? instructions = null, bool? isRequired = null, int? stepOrder = null, string? stepType = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowLineStepService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowLineStepRequest
                {
                    WorkflowLineStepID = workflowLineStepId,
                    Name = name,
                    Description = description,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    StepOrder = stepOrder,
                    StepType = stepType,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowLineStepResponse> DeleteWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowLineStepService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowLineStepRequest
                {
                    WorkflowLineStepID = workflowLineStepId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region Workflow Note Links

        public async Task<CreateVaultWorkflowNoteResponse> CreateWorkflowNoteAsync(string workflowId, string noteId, string? workflowNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowNoteService(adapter, _logger)
                .Execute(new CreateVaultWorkflowNoteRequest
                {
                    WorkflowID = workflowId,
                    NoteID = noteId,
                    WorkflowNoteID = workflowNoteId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowNoteResponse> UpdateWorkflowNoteAsync(string workflowNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowNoteService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowNoteRequest
                {
                    WorkflowNoteID = workflowNoteId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowNoteResponse> DeleteWorkflowNoteAsync(string workflowNoteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowNoteService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowNoteRequest
                {
                    WorkflowNoteID = workflowNoteId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultWorkflowNodeNoteResponse> CreateWorkflowNodeNoteAsync(string workflowNodeId, string noteId, string? workflowNodeNoteId = null, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowNodeNoteService(adapter, _logger)
                .Execute(new CreateVaultWorkflowNodeNoteRequest
                {
                    WorkflowNodeID = workflowNodeId,
                    NoteID = noteId,
                    WorkflowNodeNoteID = workflowNodeNoteId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    NoteOrder = noteOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowNodeNoteResponse> UpdateWorkflowNodeNoteAsync(string workflowNodeNoteId, string? usageRole, string? instructions, bool? isRequired, int? noteOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowNodeNoteService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowNodeNoteRequest
                {
                    WorkflowNodeNoteID = workflowNodeNoteId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    NoteOrder = noteOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowNodeNoteResponse> DeleteWorkflowNodeNoteAsync(string workflowNodeNoteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowNodeNoteService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowNodeNoteRequest
                {
                    WorkflowNodeNoteID = workflowNodeNoteId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultWorkflowLineNoteResponse> CreateWorkflowLineNoteAsync(string workflowLineId, string noteId, string? workflowLineNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowLineNoteService(adapter, _logger)
                .Execute(new CreateVaultWorkflowLineNoteRequest
                {
                    WorkflowLineID = workflowLineId,
                    NoteID = noteId,
                    WorkflowLineNoteID = workflowLineNoteId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowLineNoteResponse> UpdateWorkflowLineNoteAsync(string workflowLineNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowLineNoteService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowLineNoteRequest
                {
                    WorkflowLineNoteID = workflowLineNoteId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowLineNoteResponse> DeleteWorkflowLineNoteAsync(string workflowLineNoteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowLineNoteService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowLineNoteRequest
                {
                    WorkflowLineNoteID = workflowLineNoteId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultWorkflowLineStepNoteResponse> CreateWorkflowLineStepNoteAsync(string workflowLineStepId, string noteId, string? workflowLineStepNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowLineStepNoteService(adapter, _logger)
                .Execute(new CreateVaultWorkflowLineStepNoteRequest
                {
                    WorkflowLineStepID = workflowLineStepId,
                    NoteID = noteId,
                    WorkflowLineStepNoteID = workflowLineStepNoteId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowLineStepNoteResponse> UpdateWorkflowLineStepNoteAsync(string workflowLineStepNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowLineStepNoteService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowLineStepNoteRequest
                {
                    WorkflowLineStepNoteID = workflowLineStepNoteId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowLineStepNoteResponse> DeleteWorkflowLineStepNoteAsync(string workflowLineStepNoteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowLineStepNoteService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowLineStepNoteRequest
                {
                    WorkflowLineStepNoteID = workflowLineStepNoteId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region Workflow FileRef Links

        public async Task<CreateVaultWorkflowFileRefResponse> CreateWorkflowFileRefAsync(string workflowId, string fileRefId, string? workflowFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowFileRefService(adapter, _logger)
                .Execute(new CreateVaultWorkflowFileRefRequest
                {
                    WorkflowID = workflowId,
                    FileRefID = fileRefId,
                    WorkflowFileRefID = workflowFileRefId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowFileRefResponse> UpdateWorkflowFileRefAsync(string workflowFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowFileRefService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowFileRefRequest
                {
                    WorkflowFileRefID = workflowFileRefId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowFileRefResponse> DeleteWorkflowFileRefAsync(string workflowFileRefId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowFileRefService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowFileRefRequest
                {
                    WorkflowFileRefID = workflowFileRefId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultWorkflowNodeFileRefResponse> CreateWorkflowNodeFileRefAsync(string workflowNodeId, string fileRefId, string? workflowNodeFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowNodeFileRefService(adapter, _logger)
                .Execute(new CreateVaultWorkflowNodeFileRefRequest
                {
                    WorkflowNodeID = workflowNodeId,
                    FileRefID = fileRefId,
                    WorkflowNodeFileRefID = workflowNodeFileRefId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowNodeFileRefResponse> UpdateWorkflowNodeFileRefAsync(string workflowNodeFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowNodeFileRefService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowNodeFileRefRequest
                {
                    WorkflowNodeFileRefID = workflowNodeFileRefId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowNodeFileRefResponse> DeleteWorkflowNodeFileRefAsync(string workflowNodeFileRefId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowNodeFileRefService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowNodeFileRefRequest
                {
                    WorkflowNodeFileRefID = workflowNodeFileRefId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultWorkflowLineFileRefResponse> CreateWorkflowLineFileRefAsync(string workflowLineId, string fileRefId, string? workflowLineFileRefId = null, string? instructions = null, bool? isRequired = null, int? fileOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowLineFileRefService(adapter, _logger)
                .Execute(new CreateVaultWorkflowLineFileRefRequest
                {
                    WorkflowLineID = workflowLineId,
                    FileRefID = fileRefId,
                    WorkflowLineFileRefID = workflowLineFileRefId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    FileOrder = fileOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowLineFileRefResponse> UpdateWorkflowLineFileRefAsync(string workflowLineFileRefId, string? usageRole, string? instructions, bool? isRequired, int? fileOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowLineFileRefService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowLineFileRefRequest
                {
                    WorkflowLineFileRefID = workflowLineFileRefId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    FileOrder = fileOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowLineFileRefResponse> DeleteWorkflowLineFileRefAsync(string workflowLineFileRefId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowLineFileRefService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowLineFileRefRequest
                {
                    WorkflowLineFileRefID = workflowLineFileRefId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultWorkflowLineStepFileRefResponse> CreateWorkflowLineStepFileRefAsync(string workflowLineStepId, string fileRefId, string? workflowLineStepFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultWorkflowLineStepFileRefService(adapter, _logger)
                .Execute(new CreateVaultWorkflowLineStepFileRefRequest
                {
                    WorkflowLineStepID = workflowLineStepId,
                    FileRefID = fileRefId,
                    WorkflowLineStepFileRefID = workflowLineStepFileRefId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultWorkflowLineStepFileRefResponse> UpdateWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultWorkflowLineStepFileRefService(adapter, _logger)
                .Execute(new UpdateVaultWorkflowLineStepFileRefRequest
                {
                    WorkflowLineStepFileRefID = workflowLineStepFileRefId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultWorkflowLineStepFileRefResponse> DeleteWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultWorkflowLineStepFileRefService(adapter, _logger)
                .Execute(new DeleteVaultWorkflowLineStepFileRefRequest
                {
                    WorkflowLineStepFileRefID = workflowLineStepFileRefId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region Vault / Project / Session Note Links

        public async Task<CreateVaultHeaderNoteResponse> CreateVaultHeaderNoteAsync(string vaultId, string noteId, string? headerNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultHeaderNoteService(adapter, _logger)
                .Execute(new CreateVaultHeaderNoteRequest
                {
                    VaultID = vaultId,
                    NoteID = noteId,
                    HeaderNoteID = headerNoteId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultHeaderNoteResponse> UpdateVaultHeaderNoteAsync(string headerNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultHeaderNoteService(adapter, _logger)
                .Execute(new UpdateVaultHeaderNoteRequest
                {
                    HeaderNoteID = headerNoteId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultHeaderNoteResponse> DeleteVaultHeaderNoteAsync(string headerNoteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultHeaderNoteService(adapter, _logger)
                .Execute(new DeleteVaultHeaderNoteRequest
                {
                    HeaderNoteID = headerNoteId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultProjectNoteResponse> CreateVaultProjectNoteAsync(string projectId, string noteId, string? projectNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultProjectNoteService(adapter, _logger)
                .Execute(new CreateVaultProjectNoteRequest
                {
                    ProjectID = projectId,
                    NoteID = noteId,
                    ProjectNoteID = projectNoteId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultProjectNoteResponse> UpdateVaultProjectNoteAsync(string projectNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultProjectNoteService(adapter, _logger)
                .Execute(new UpdateVaultProjectNoteRequest
                {
                    ProjectNoteID = projectNoteId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultProjectNoteResponse> DeleteVaultProjectNoteAsync(string projectNoteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultProjectNoteService(adapter, _logger)
                .Execute(new DeleteVaultProjectNoteRequest
                {
                    ProjectNoteID = projectNoteId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultSessionNoteResponse> CreateVaultSessionNoteAsync(string sessionId, string noteId, string? sessionNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultSessionNoteService(adapter, _logger)
                .Execute(new CreateVaultSessionNoteRequest
                {
                    SessionID = sessionId,
                    NoteID = noteId,
                    SessionNoteID = sessionNoteId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultSessionNoteResponse> UpdateVaultSessionNoteAsync(string sessionNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultSessionNoteService(adapter, _logger)
                .Execute(new UpdateVaultSessionNoteRequest
                {
                    SessionNoteID = sessionNoteId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultSessionNoteResponse> DeleteVaultSessionNoteAsync(string sessionNoteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultSessionNoteService(adapter, _logger)
                .Execute(new DeleteVaultSessionNoteRequest
                {
                    SessionNoteID = sessionNoteId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region Vault / Project / Session FileRef Links

        public async Task<CreateVaultHeaderFileRefResponse> CreateVaultHeaderFileRefAsync(string vaultId, string fileRefId, string? headerFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultHeaderFileRefService(adapter, _logger)
                .Execute(new CreateVaultHeaderFileRefRequest
                {
                    VaultID = vaultId,
                    FileRefID = fileRefId,
                    HeaderFileRefID = headerFileRefId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultHeaderFileRefResponse> UpdateVaultHeaderFileRefAsync(string headerFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultHeaderFileRefService(adapter, _logger)
                .Execute(new UpdateVaultHeaderFileRefRequest
                {
                    HeaderFileRefID = headerFileRefId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultHeaderFileRefResponse> DeleteVaultHeaderFileRefAsync(string headerFileRefId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultHeaderFileRefService(adapter, _logger)
                .Execute(new DeleteVaultHeaderFileRefRequest
                {
                    HeaderFileRefID = headerFileRefId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultProjectFileRefResponse> CreateVaultProjectFileRefAsync(string projectId, string fileRefId, string? projectFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultProjectFileRefService(adapter, _logger)
                .Execute(new CreateVaultProjectFileRefRequest
                {
                    ProjectID = projectId,
                    FileRefID = fileRefId,
                    ProjectFileRefID = projectFileRefId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultProjectFileRefResponse> UpdateVaultProjectFileRefAsync(string projectFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultProjectFileRefService(adapter, _logger)
                .Execute(new UpdateVaultProjectFileRefRequest
                {
                    ProjectFileRefID = projectFileRefId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultProjectFileRefResponse> DeleteVaultProjectFileRefAsync(string projectFileRefId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultProjectFileRefService(adapter, _logger)
                .Execute(new DeleteVaultProjectFileRefRequest
                {
                    ProjectFileRefID = projectFileRefId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultSessionFileRefResponse> CreateVaultSessionFileRefAsync(string sessionId, string fileRefId, string? sessionFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultSessionFileRefService(adapter, _logger)
                .Execute(new CreateVaultSessionFileRefRequest
                {
                    SessionID = sessionId,
                    FileRefID = fileRefId,
                    SessionFileRefID = sessionFileRefId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultSessionFileRefResponse> UpdateVaultSessionFileRefAsync(string sessionFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultSessionFileRefService(adapter, _logger)
                .Execute(new UpdateVaultSessionFileRefRequest
                {
                    SessionFileRefID = sessionFileRefId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultSessionFileRefResponse> DeleteVaultSessionFileRefAsync(string sessionFileRefId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultSessionFileRefService(adapter, _logger)
                .Execute(new DeleteVaultSessionFileRefRequest
                {
                    SessionFileRefID = sessionFileRefId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region FileRefs

        public async Task<CreateVaultFileRefResponse> CreateFileRefAsync(string vaultId, string name, string path, string? fileRefId = null, string? projectId = null, string? sessionId = null, long? fileSizeBytes = null, string? contentHash = null, string? mimeType = null, int? fileOrder = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultFileRefService(adapter, _logger)
                .Execute(new CreateVaultFileRefRequest
                {
                    VaultID = vaultId,
                    Name = name,
                    Path = path,
                    FileRefID = fileRefId,
                    ProjectID = projectId,
                    SessionID = sessionId,
                    FileSizeBytes = fileSizeBytes,
                    ContentHash = contentHash,
                    MimeType = mimeType,
                    FileOrder = fileOrder ?? 0,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultFileRefResponse> UpdateFileRefAsync(string fileRefId, string? name = null, string? path = null, string? mimeType = null, string? contentHash = null, long? fileSizeBytes = null, int? fileOrder = null, string? vaultId = null, string? projectId = null, string? sessionId = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultFileRefService(adapter, _logger)
                .Execute(new UpdateVaultFileRefRequest
                {
                    FileRefID = fileRefId,
                    Name = name,
                    Path = path,
                    MimeType = mimeType,
                    ContentHash = contentHash,
                    FileSizeBytes = fileSizeBytes,
                    FileOrder = fileOrder,
                    VaultID = vaultId,
                    ProjectID = projectId,
                    SessionID = sessionId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultFileRefResponse> DeleteFileRefAsync(string fileRefId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultFileRefService(adapter, _logger)
                .Execute(new DeleteVaultFileRefRequest
                {
                    FileRefID = fileRefId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region FileRef Note / Note FileRef Links

        public async Task<CreateVaultFileRefNoteResponse> CreateFileRefNoteAsync(string fileRefId, string noteId, string? fileRefNoteId = null, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultFileRefNoteService(adapter, _logger)
                .Execute(new CreateVaultFileRefNoteRequest
                {
                    FileRefID = fileRefId,
                    NoteID = noteId,
                    FileRefNoteID = fileRefNoteId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    NoteOrder = noteOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultFileRefNoteResponse> UpdateFileRefNoteAsync(string fileRefNoteId, string? usageRole, string? instructions, bool? isRequired, int? noteOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultFileRefNoteService(adapter, _logger)
                .Execute(new UpdateVaultFileRefNoteRequest
                {
                    FileRefNoteID = fileRefNoteId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    NoteOrder = noteOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultFileRefNoteResponse> DeleteFileRefNoteAsync(string fileRefNoteId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultFileRefNoteService(adapter, _logger)
                .Execute(new DeleteVaultFileRefNoteRequest
                {
                    FileRefNoteID = fileRefNoteId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultNoteFileRefResponse> CreateNoteFileRefAsync(string noteId, string fileRefId, string? noteFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultNoteFileRefService(adapter, _logger)
                .Execute(new CreateVaultNoteFileRefRequest
                {
                    NoteID = noteId,
                    FileRefID = fileRefId,
                    NoteFileRefID = noteFileRefId,
                    Instructions = instructions,
                    IsRequired = isRequired ?? false,
                    SortOrder = sortOrder ?? 0,
                    UsageRole = usageRole,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultNoteFileRefResponse> UpdateNoteFileRefAsync(string noteFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultNoteFileRefService(adapter, _logger)
                .Execute(new UpdateVaultNoteFileRefRequest
                {
                    NoteFileRefID = noteFileRefId,
                    UsageRole = usageRole,
                    Instructions = instructions,
                    IsRequired = isRequired,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultNoteFileRefResponse> DeleteNoteFileRefAsync(string noteFileRefId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultNoteFileRefService(adapter, _logger)
                .Execute(new DeleteVaultNoteFileRefRequest
                {
                    NoteFileRefID = noteFileRefId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region FileRef Relations

        public async Task<CreateVaultFileRefRelationResponse> CreateFileRefRelationAsync(string sourceFileRefId, string targetFileRefId, string relationType, string? description = null, string? fileRefRelationId = null, int? sortOrder = null, float? weight = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultFileRefRelationService(adapter, _logger)
                .Execute(new CreateVaultFileRefRelationRequest
                {
                    SourceFileRefID = sourceFileRefId,
                    TargetFileRefID = targetFileRefId,
                    RelationType = relationType,
                    Description = description,
                    FileRefRelationID = fileRefRelationId,
                    SortOrder = sortOrder ?? 0,
                    Weight = weight ?? 0f,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultFileRefRelationResponse> UpdateFileRefRelationAsync(string fileRefRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultFileRefRelationService(adapter, _logger)
                .Execute(new UpdateVaultFileRefRelationRequest
                {
                    FileRefRelationID = fileRefRelationId,
                    RelationType = relationType,
                    Description = description,
                    SortOrder = sortOrder,
                    Weight = weight,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultFileRefRelationResponse> DeleteFileRefRelationAsync(string fileRefRelationId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultFileRefRelationService(adapter, _logger)
                .Execute(new DeleteVaultFileRefRelationRequest
                {
                    FileRefRelationID = fileRefRelationId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion

        #region Note Relations / Tags / Metadata

        public async Task<CreateVaultNoteRelationResponse> CreateNoteRelationAsync(string sourceNoteId, string targetNoteId, string relationType, string? description = null, string? noteRelationId = null, int? sortOrder = null, float? weight = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultNoteRelationService(adapter, _logger)
                .Execute(new CreateVaultNoteRelationRequest
                {
                    SourceNoteID = sourceNoteId,
                    TargetNoteID = targetNoteId,
                    RelationType = relationType,
                    Description = description,
                    NoteRelationID = noteRelationId,
                    SortOrder = sortOrder ?? 0,
                    Weight = weight ?? 0f,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultNoteRelationResponse> UpdateNoteRelationAsync(string noteRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultNoteRelationService(adapter, _logger)
                .Execute(new UpdateVaultNoteRelationRequest
                {
                    NoteRelationID = noteRelationId,
                    RelationType = relationType,
                    Description = description,
                    SortOrder = sortOrder,
                    Weight = weight,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultNoteRelationResponse> DeleteNoteRelationAsync(string noteRelationId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultNoteRelationService(adapter, _logger)
                .Execute(new DeleteVaultNoteRelationRequest
                {
                    NoteRelationID = noteRelationId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultNoteVaultTagResponse> CreateNoteVaultTagAsync(string noteId, string tagId, string? noteVaultTagId = null, int? sortOrder = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultNoteVaultTagService(adapter, _logger)
                .Execute(new CreateVaultNoteVaultTagRequest
                {
                    NoteID = noteId,
                    TagID = tagId,
                    NoteVaultTagID = noteVaultTagId,
                    SortOrder = sortOrder ?? 0,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultNoteVaultTagResponse> UpdateNoteVaultTagAsync(string noteVaultTagId, int? sortOrder = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultNoteVaultTagService(adapter, _logger)
                .Execute(new UpdateVaultNoteVaultTagRequest
                {
                    NoteVaultTagID = noteVaultTagId,
                    SortOrder = sortOrder,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultNoteVaultTagResponse> DeleteNoteVaultTagAsync(string noteVaultTagId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultNoteVaultTagService(adapter, _logger)
                .Execute(new DeleteVaultNoteVaultTagRequest
                {
                    NoteVaultTagID = noteVaultTagId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultTagResponse> UpdateTagAsync(string tagId, string? name = null, string? description = null, string? colorHex = null, int? sortOrder = null, string? vaultId = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultTagService(adapter, _logger)
                .Execute(new UpdateVaultTagRequest
                {
                    TagID = tagId,
                    Name = name,
                    Description = description,
                    Color = colorHex,
                    VaultID = vaultId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<CreateVaultMetadataResponse> CreateMetadataAsync(string noteId, string key, string? value = null, string? metadataId = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new CreateVaultMetadataService(adapter, _logger)
                .Execute(new CreateVaultMetadataRequest
                {
                    NoteID = noteId,
                    Key = key,
                    Value = value,
                    MetadataID = metadataId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<UpdateVaultMetadataResponse> UpdateMetadataAsync(string metadataId, string? key = null, string? value = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new UpdateVaultMetadataService(adapter, _logger)
                .Execute(new UpdateVaultMetadataRequest
                {
                    MetadataID = metadataId,
                    Key = key,
                    Value = value,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultMetadataResponse> DeleteMetadataAsync(string metadataId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter = new VaultDbContextAdapter(db);
            return new DeleteVaultMetadataService(adapter, _logger)
                .Execute(new DeleteVaultMetadataRequest
                {
                    MetadataID = metadataId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        #endregion
    }
}
