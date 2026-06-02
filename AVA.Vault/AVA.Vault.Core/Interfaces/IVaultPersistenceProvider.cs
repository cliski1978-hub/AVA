using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Services.Data;


namespace AVA.Vault.Core.Interfaces
{
    /// <summary>
    /// Storage-neutral persistence contract for Vault, Project, and Session operations.
    /// All write operations return the CfkApiResponse-derived type from the underlying service —
    /// Succeeded and UserMessage travel unmodified to the calling ViewModel.
    /// Read/List operations return entity collections directly.
    /// </summary>
    public interface IVaultPersistenceProvider
    {
        // ── Vault ──────────────────────────────────────────────────────────────
        Task<CreateVaultHeaderResponse>  CreateVaultAsync(string name, string? vaultId = null, CancellationToken ct = default);
        Task<UpdateVaultHeaderResponse>  RenameVaultAsync(string vaultId, string newName, CancellationToken ct = default);
        Task<DeleteVaultHeaderResponse>  DeleteVaultAsync(string vaultId, CancellationToken ct = default);
        Task<IEnumerable<VaultHeader>>   ListVaultsAsync(CancellationToken ct = default);

        // ── Project ────────────────────────────────────────────────────────────
        Task<CreateVaultProjectResponse> CreateProjectAsync(string vaultId, string name, string? projectId = null, CancellationToken ct = default);
        Task<UpdateVaultProjectResponse> RenameProjectAsync(string vaultId, string projectId, string newName, CancellationToken ct = default);
        Task<DeleteVaultProjectResponse> DeleteProjectAsync(string vaultId, string projectId, CancellationToken ct = default);
        Task<IEnumerable<VaultProject>>  ListProjectsAsync(string vaultId, CancellationToken ct = default);

        // ── Session ────────────────────────────────────────────────────────────
        Task<CreateVaultSessionResponse>  CreateSessionAsync(string vaultId, string? projectId, string name, string? sessionId = null, CancellationToken ct = default);
        Task<UpdateVaultSessionResponse>  RenameSessionAsync(string vaultId, string sessionId, string newName, CancellationToken ct = default);
        Task<UpdateVaultSessionResponse>  UpdateSessionModelStateAsync(string vaultId, string sessionId, List<string> attachedModelIds, List<string> broadcastGroupIds, string? defaultModelId, CancellationToken ct = default);
        Task<DeleteVaultSessionResponse>  DeleteSessionAsync(string vaultId, string sessionId, CancellationToken ct = default);
        Task<IEnumerable<VaultSession>>   ListSessionsAsync(string vaultId, CancellationToken ct = default);

        // ── Note ───────────────────────────────────────────────────────────────
        Task<CreateVaultNoteResponse>    CreateNoteAsync(string vaultId, string? projectId, string title, string content, string? sessionId = null, CancellationToken ct = default);
        Task<VaultNote?>                 GetNoteAsync(string vaultId, string noteId, CancellationToken ct = default);
        Task<UpdateVaultNoteResponse>    UpdateNoteAsync(string vaultId, string noteId, string? title, string? content, CancellationToken ct = default);
        Task<DeleteVaultNoteResponse>    DeleteNoteAsync(string vaultId, string noteId, CancellationToken ct = default);
        Task<IEnumerable<VaultNote>>     SearchNotesAsync(string vaultId, string? projectId = null, string? sessionId = null, string? keyword = null, string? tag = null, string sortBy = "Updated", bool sortDescending = true, DateTime? createdAfter = null, DateTime? createdBefore = null, DateTime? updatedAfter = null, DateTime? updatedBefore = null, CancellationToken ct = default);

        // ── Tag ────────────────────────────────────────────────────────────────
        Task<CreateVaultTagResponse>     CreateTagAsync(string vaultId, string name, string? color = null, CancellationToken ct = default);
        Task<UpdateVaultTagResponse>     RenameTagAsync(string vaultId, string tagId, string newName, CancellationToken ct = default);
        Task<DeleteVaultTagResponse>     DeleteTagAsync(string vaultId, string tagId, CancellationToken ct = default);
        Task<IEnumerable<VaultTag>>      ListTagsAsync(string vaultId, CancellationToken ct = default);
        Task<AssignTagToNoteResponse>    AssignTagToNoteAsync(string vaultId, string noteId, string tagId, CancellationToken ct = default);
        Task<RemoveTagFromNoteResponse>  RemoveTagFromNoteAsync(string vaultId, string noteId, string tagId, CancellationToken ct = default);

        // ── Link ───────────────────────────────────────────────────────────────
        Task<CreateVaultRelationResponse>           CreateRelationAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string? description = null, CancellationToken ct = default);
        Task<DeleteVaultLinkResponse>           DeleteRelationAsync(string vaultId, string linkId, CancellationToken ct = default);
        Task<IEnumerable<RelatedNoteResult>>    GetRelatedNotesAsync(string vaultId, string noteId, string? relationType = null, CancellationToken ct = default);
    }
}
