using AVA.UI.CORE.Models.UI;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Services.Data;
using AVA.Vault.Core.Services.Data.VaultProjects;

namespace AVA.UI.Vault.Services;

/// <summary>
/// UI-facing bridge between ViewModels and AVA.Vault.Core.
/// All write operations return the CfkApiResponse-derived type from the underlying service —
/// Succeeded and UserMessage travel unmodified to the calling ViewModel for error ribbon display.
/// Read/List/Search operations return entity types directly.
/// </summary>
public interface IVaultUiSyncService
{
    // ── Infrastructure ────────────────────────────────────────────────────────
    Task EnsureVaultInfrastructureAsync();
    Task EnsureVaultExistsAsync(VaultState vaultState);
    Task EnsureProjectExistsAsync(VaultState vaultState, ProjectState projectState);

    // ── Startup hydration ─────────────────────────────────────────────────────
    Task<List<VaultState>> LoadVaultsFromDatabaseAsync();
    Task<List<VaultState>> LoadVaultsFromFileSystemAsync();

    // ── Vault ─────────────────────────────────────────────────────────────────
    Task<CreateVaultHeaderResponse>  CreateVaultAsync(string name, string storageMode, string? vaultId = null);
    Task<UpdateVaultHeaderResponse>  RenameVaultAsync(string vaultId, string newName, string storageMode);
    Task<DeleteVaultHeaderResponse>  DeleteVaultAsync(string vaultId, string storageMode);

    // ── Project ───────────────────────────────────────────────────────────────
    Task<CreateVaultProjectResponse> CreateProjectAsync(string vaultId, string name, string storageMode, string? projectId = null);
    Task<UpdateVaultProjectResponse> RenameProjectAsync(string vaultId, string projectId, string newName, string storageMode);
    Task<DeleteVaultProjectResponse> DeleteProjectAsync(string vaultId, string projectId, string storageMode);

    // ── Session ───────────────────────────────────────────────────────────────
    Task<CreateVaultSessionResponse>  CreateSessionAsync(string vaultId, string? projectId, string name, string storageMode, string? sessionId = null);
    Task<UpdateVaultSessionResponse>  RenameSessionAsync(string vaultId, string sessionId, string newName, string storageMode);
    Task<UpdateVaultSessionResponse>  UpdateSessionModelStateAsync(string vaultId, string sessionId, string storageMode, List<string> attachedModelIds, List<string> broadcastGroupIds, string? defaultModelId);
    Task<DeleteVaultSessionResponse>  DeleteSessionAsync(string vaultId, string sessionId, string storageMode);

    // ── Note ──────────────────────────────────────────────────────────────────
    Task<CreateVaultNoteResponse>    CreateNoteAsync(string vaultId, string projectId, string title, string content, string storageMode, string? sessionId = null);
    Task<VaultNote?>                 GetNoteAsync(string vaultId, string noteId, string storageMode);
    Task<UpdateVaultNoteResponse>    UpdateNoteAsync(string vaultId, string noteId, string? title, string? content, string storageMode);
    Task<DeleteVaultNoteResponse>    DeleteNoteAsync(string vaultId, string noteId, string storageMode);

    // ── Tag ───────────────────────────────────────────────────────────────────
    Task<CreateVaultTagResponse>     CreateTagAsync(string vaultId, string name, string storageMode, string? color = null);
    Task<DeleteVaultTagResponse>     DeleteTagAsync(string vaultId, string tagId, string storageMode);
    Task<IEnumerable<VaultTag>>      ListTagsAsync(string vaultId, string storageMode);
    Task<AssignTagToNoteResponse>    AssignTagToNoteAsync(string vaultId, string noteId, string tagId, string storageMode);
    Task<RemoveTagFromNoteResponse>  RemoveTagFromNoteAsync(string vaultId, string noteId, string tagId, string storageMode);

    // ── Link ──────────────────────────────────────────────────────────────────
    Task<CreateVaultLinkResponse>         CreateLinkAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string storageMode, string? description = null);
    Task<DeleteVaultLinkResponse>         DeleteLinkAsync(string vaultId, string linkId, string storageMode);
    Task<IEnumerable<RelatedNoteResult>>  GetRelatedNotesAsync(string vaultId, string noteId, string storageMode, string? relationType = null);

    // ── Memory sync ───────────────────────────────────────────────────────────
    Task PushNoteToMemoryAsync(VaultNote note, CancellationToken ct = default);

    // ── Search ────────────────────────────────────────────────────────────────
    Task<List<VaultNote>> SearchNotesAsync(
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
        CancellationToken ct    = default);
}
