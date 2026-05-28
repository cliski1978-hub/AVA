using Microsoft.EntityFrameworkCore;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Data;
using AVA.Vault.Core.Services.Data.VaultProjects;

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
                    SessionId        = sessionId,
                    VaultId          = vaultId,
                    ProjectId        = projectId,
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
                    VaultId          = vaultId,
                    SessionId        = sessionId,
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
                    VaultId               = vaultId,
                    SessionId             = sessionId,
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
                    VaultId          = vaultId,
                    SessionId        = sessionId,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<IEnumerable<VaultSession>> ListSessionsAsync(string vaultId, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            return await db.VaultSessions.Where(s => s.VaultID == vaultId).OrderBy(s => s.CreatedAt).ToListAsync(ct);
        }

        // ── Note ───────────────────────────────────────────────────────────────

        public async Task<CreateVaultNoteResponse> CreateNoteAsync(string vaultId, string projectId, string title, string content, string? sessionId = null, CancellationToken ct = default)
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

        public async Task<VaultNote?> GetNoteAsync(string vaultId, string noteId, CancellationToken ct = default)
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

        public async Task<UpdateVaultNoteResponse> UpdateNoteAsync(string vaultId, string noteId, string? title, string? content, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new UpdateVaultNoteService(adapter, _logger)
                .Execute(new UpdateVaultNoteRequest
                {
                    VaultID          = vaultId,
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
                    VaultID          = vaultId,
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

        public async Task<CreateVaultLinkResponse> CreateLinkAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string? description = null, CancellationToken ct = default)
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var adapter        = new VaultDbContextAdapter(db);
            return new CreateVaultLinkService(adapter, _logger, _ids)
                .Execute(new CreateVaultLinkRequest
                {
                    VaultID          = vaultId,
                    SourceNoteID     = sourceNoteId,
                    TargetNoteID     = targetNoteId,
                    RelationType     = relationType,
                    Description      = description,
                    RequestPartyName = "AVA.Vault"
                });
        }

        public async Task<DeleteVaultLinkResponse> DeleteLinkAsync(string vaultId, string linkId, CancellationToken ct = default)
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
    }
}
