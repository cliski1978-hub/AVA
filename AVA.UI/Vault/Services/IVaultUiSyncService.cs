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

    // ── Vault ─────────────────────────────────────────────────────────────────
    Task<CreateVaultHeaderResponse>  CreateVaultAsync(string name, string? vaultId = null);
    Task<UpdateVaultHeaderResponse>  RenameVaultAsync(string vaultId, string newName);
    Task<DeleteVaultHeaderResponse>  DeleteVaultAsync(string vaultId);

    // ── Project ───────────────────────────────────────────────────────────────
    Task<CreateVaultProjectResponse> CreateProjectAsync(string vaultId, string name, string? projectId = null);
    Task<UpdateVaultProjectResponse> RenameProjectAsync(string vaultId, string projectId, string newName);
    Task<DeleteVaultProjectResponse> DeleteProjectAsync(string vaultId, string projectId);

    // ── Session ───────────────────────────────────────────────────────────────
    Task<CreateVaultSessionResponse>  CreateSessionAsync(string vaultId, string? projectId, string name, string? sessionId = null);
    Task<UpdateVaultSessionResponse>  RenameSessionAsync(string vaultId, string sessionId, string newName);
    Task<UpdateVaultSessionResponse>  UpdateSessionModelStateAsync(string vaultId, string sessionId, List<string> attachedModelIds, List<string> broadcastGroupIds, string? defaultModelId);
    Task<DeleteVaultSessionResponse>  DeleteSessionAsync(string vaultId, string sessionId);

    // ── Note ──────────────────────────────────────────────────────────────────
    Task<CreateVaultNoteResponse>    CreateNoteAsync(string vaultId, string projectId, string title, string content, string? sessionId = null);
    Task<VaultNote?>                 GetNoteAsync(string vaultId, string noteId);
    Task<UpdateVaultNoteResponse>    UpdateNoteAsync(string vaultId, string noteId, string? title, string? content);
    Task<DeleteVaultNoteResponse>    DeleteNoteAsync(string vaultId, string noteId);

    // ── Tag ───────────────────────────────────────────────────────────────────
    Task<CreateVaultTagResponse>     CreateTagAsync(string vaultId, string name, string? color = null);
    Task<DeleteVaultTagResponse>     DeleteTagAsync(string vaultId, string tagId);
    Task<IEnumerable<VaultTag>>      ListTagsAsync(string vaultId);
    Task<AssignTagToNoteResponse>    AssignTagToNoteAsync(string vaultId, string noteId, string tagId);
    Task<RemoveTagFromNoteResponse>  RemoveTagFromNoteAsync(string vaultId, string noteId, string tagId);

    // ── Link ──────────────────────────────────────────────────────────────────
    Task<CreateVaultLinkResponse>         CreateLinkAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string? description = null);
    Task<DeleteVaultLinkResponse>         DeleteLinkAsync(string vaultId, string linkId);
    Task<IEnumerable<RelatedNoteResult>>  GetRelatedNotesAsync(string vaultId, string noteId, string? relationType = null);

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
        CancellationToken ct    = default);
}
