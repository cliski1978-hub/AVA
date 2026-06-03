using AVA.UI.CORE.Models.UI;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Services.Data;


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
    Task<CreateVaultNoteResponse>    CreateNoteAsync(string vaultId, string? projectId, string title, string content, string? sessionId = null);
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
    Task<CreateVaultRelationResponse>     CreateRelationAsync(string vaultId, string sourceNoteId, string targetNoteId, string relationType, string? description = null);
    Task<DeleteVaultLinkResponse>         DeleteRelationAsync(string vaultId, string linkId);
    Task<IEnumerable<RelatedNoteResult>>  GetRelatedNotesAsync(string vaultId, string noteId, string? relationType = null);

    // ── Memory sync ───────────────────────────────────────────────────────────
    Task PushNoteToMemoryAsync(VaultNote note, CancellationToken ct = default);

    // ── Vault Workflow ────────────────────────────────────────────────────────
    Task<CreateVaultWorkflowResponse> CreateWorkflowAsync(string vaultId, string? projectId, string name);
    Task<UpdateVaultWorkflowResponse> UpdateWorkflowAsync(string workflowId, string? name, string? description = null, string? status = null, int? sortOrder = null);
    Task<DeleteVaultWorkflowResponse> DeleteWorkflowAsync(string workflowId);

    // ── Vault Workflow Nodes ─────────────────────────────────────────────────
    Task<CreateVaultWorkflowNodeResponse> CreateWorkflowNodeAsync(string workflowId, string name, string? workflowNodeId = null, string? description = null, string? instructions = null, string? metadataJson = null, string? nodeType = null, int? nodeOrder = null, string? status = null);
    Task<UpdateVaultWorkflowNodeResponse> UpdateWorkflowNodeAsync(string workflowNodeId, string? name = null, string? description = null, string? instructions = null, string? nodeType = null, int? nodeOrder = null, string? status = null);
    Task<DeleteVaultWorkflowNodeResponse> DeleteWorkflowNodeAsync(string workflowNodeId);

    // ── Vault Workflow Lines ─────────────────────────────────────────────────
    Task<CreateVaultWorkflowLineResponse> CreateWorkflowLineAsync(string workflowId, string sourceWorkflowNodeId, string targetWorkflowNodeId, string name, string? workflowLineId = null, string? description = null, string? conditionJson = null, bool? isDefaultLine = null, string? lineType = null, int? lineOrder = null);
    Task<UpdateVaultWorkflowLineResponse> UpdateWorkflowLineAsync(string workflowLineId, string? name = null, string? description = null, string? conditionJson = null, bool? isDefaultLine = null, string? lineType = null, int? lineOrder = null);
    Task<DeleteVaultWorkflowLineResponse> DeleteWorkflowLineAsync(string workflowLineId);

    // ── Vault Workflow Line Steps ────────────────────────────────────────────
    Task<CreateVaultWorkflowLineStepResponse> CreateWorkflowLineStepAsync(string workflowLineId, string name, string? workflowLineStepId = null, string? description = null, string? instructions = null, bool? isRequired = null, int? stepOrder = null, string? stepType = null);
    Task<UpdateVaultWorkflowLineStepResponse> UpdateWorkflowLineStepAsync(string workflowLineStepId, string? name = null, string? description = null, string? instructions = null, bool? isRequired = null, int? stepOrder = null, string? stepType = null);
    Task<DeleteVaultWorkflowLineStepResponse> DeleteWorkflowLineStepAsync(string workflowLineStepId);

    // ── Workflow Note Links ──────────────────────────────────────────────────
    Task<CreateVaultWorkflowNoteResponse> CreateWorkflowNoteAsync(string workflowId, string noteId, string? workflowNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultWorkflowNoteResponse> UpdateWorkflowNoteAsync(string workflowNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultWorkflowNoteResponse> DeleteWorkflowNoteAsync(string workflowNoteId);

    Task<CreateVaultWorkflowNodeNoteResponse> CreateWorkflowNodeNoteAsync(string workflowNodeId, string noteId, string? workflowNodeNoteId = null, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null);
    Task<UpdateVaultWorkflowNodeNoteResponse> UpdateWorkflowNodeNoteAsync(string workflowNodeNoteId, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null);
    Task<DeleteVaultWorkflowNodeNoteResponse> DeleteWorkflowNodeNoteAsync(string workflowNodeNoteId);

    Task<CreateVaultWorkflowLineNoteResponse> CreateWorkflowLineNoteAsync(string workflowLineId, string noteId, string? workflowLineNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultWorkflowLineNoteResponse> UpdateWorkflowLineNoteAsync(string workflowLineNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultWorkflowLineNoteResponse> DeleteWorkflowLineNoteAsync(string workflowLineNoteId);

    Task<CreateVaultWorkflowLineStepNoteResponse> CreateWorkflowLineStepNoteAsync(string workflowLineStepId, string noteId, string? workflowLineStepNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultWorkflowLineStepNoteResponse> UpdateWorkflowLineStepNoteAsync(string workflowLineStepNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultWorkflowLineStepNoteResponse> DeleteWorkflowLineStepNoteAsync(string workflowLineStepNoteId);

    // ── Workflow FileRef Links ───────────────────────────────────────────────
    Task<CreateVaultWorkflowFileRefResponse> CreateWorkflowFileRefAsync(string workflowId, string fileRefId, string? workflowFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultWorkflowFileRefResponse> UpdateWorkflowFileRefAsync(string workflowFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultWorkflowFileRefResponse> DeleteWorkflowFileRefAsync(string workflowFileRefId);

    Task<CreateVaultWorkflowNodeFileRefResponse> CreateWorkflowNodeFileRefAsync(string workflowNodeId, string fileRefId, string? workflowNodeFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultWorkflowNodeFileRefResponse> UpdateWorkflowNodeFileRefAsync(string workflowNodeFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultWorkflowNodeFileRefResponse> DeleteWorkflowNodeFileRefAsync(string workflowNodeFileRefId);

    Task<CreateVaultWorkflowLineFileRefResponse> CreateWorkflowLineFileRefAsync(string workflowLineId, string fileRefId, string? workflowLineFileRefId = null, string? instructions = null, bool? isRequired = null, int? fileOrder = null, string? usageRole = null);
    Task<UpdateVaultWorkflowLineFileRefResponse> UpdateWorkflowLineFileRefAsync(string workflowLineFileRefId, string? instructions = null, bool? isRequired = null, int? fileOrder = null, string? usageRole = null);
    Task<DeleteVaultWorkflowLineFileRefResponse> DeleteWorkflowLineFileRefAsync(string workflowLineFileRefId);

    Task<CreateVaultWorkflowLineStepFileRefResponse> CreateWorkflowLineStepFileRefAsync(string workflowLineStepId, string fileRefId, string? workflowLineStepFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultWorkflowLineStepFileRefResponse> UpdateWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultWorkflowLineStepFileRefResponse> DeleteWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId);

    // ── Vault / Project / Session Note Links ─────────────────────────────────
    Task<CreateVaultHeaderNoteResponse> CreateVaultHeaderNoteAsync(string vaultId, string noteId, string? headerNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultHeaderNoteResponse> UpdateVaultHeaderNoteAsync(string headerNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultHeaderNoteResponse> DeleteVaultHeaderNoteAsync(string headerNoteId);

    Task<CreateVaultProjectNoteResponse> CreateVaultProjectNoteAsync(string projectId, string noteId, string? projectNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultProjectNoteResponse> UpdateVaultProjectNoteAsync(string projectNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultProjectNoteResponse> DeleteVaultProjectNoteAsync(string projectNoteId);

    Task<CreateVaultSessionNoteResponse> CreateVaultSessionNoteAsync(string sessionId, string noteId, string? sessionNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultSessionNoteResponse> UpdateVaultSessionNoteAsync(string sessionNoteId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultSessionNoteResponse> DeleteVaultSessionNoteAsync(string sessionNoteId);

    // ── Vault / Project / Session FileRef Links ──────────────────────────────
    Task<CreateVaultHeaderFileRefResponse> CreateVaultHeaderFileRefAsync(string vaultId, string fileRefId, string? headerFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultHeaderFileRefResponse> UpdateVaultHeaderFileRefAsync(string headerFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultHeaderFileRefResponse> DeleteVaultHeaderFileRefAsync(string headerFileRefId);

    Task<CreateVaultProjectFileRefResponse> CreateVaultProjectFileRefAsync(string projectId, string fileRefId, string? projectFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultProjectFileRefResponse> UpdateVaultProjectFileRefAsync(string projectFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultProjectFileRefResponse> DeleteVaultProjectFileRefAsync(string projectFileRefId);

    Task<CreateVaultSessionFileRefResponse> CreateVaultSessionFileRefAsync(string sessionId, string fileRefId, string? sessionFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultSessionFileRefResponse> UpdateVaultSessionFileRefAsync(string sessionFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultSessionFileRefResponse> DeleteVaultSessionFileRefAsync(string sessionFileRefId);

    // ── FileRefs ─────────────────────────────────────────────────────────────
    Task<CreateVaultFileRefResponse> CreateFileRefAsync(string vaultId, string name, string path, string? fileRefId = null, string? projectId = null, string? sessionId = null, long? fileSizeBytes = null, string? contentHash = null, string? mimeType = null, int? fileOrder = null);
    Task<UpdateVaultFileRefResponse> UpdateFileRefAsync(string fileRefId, string? name = null, string? path = null, string? mimeType = null, string? contentHash = null, long? fileSizeBytes = null, int? fileOrder = null, string? vaultId = null, string? projectId = null, string? sessionId = null);
    Task<DeleteVaultFileRefResponse> DeleteFileRefAsync(string fileRefId);

    Task<CreateVaultFileRefNoteResponse> CreateFileRefNoteAsync(string fileRefId, string noteId, string? fileRefNoteId = null, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null);
    Task<UpdateVaultFileRefNoteResponse> UpdateFileRefNoteAsync(string fileRefNoteId, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null);
    Task<DeleteVaultFileRefNoteResponse> DeleteFileRefNoteAsync(string fileRefNoteId);

    Task<CreateVaultNoteFileRefResponse> CreateNoteFileRefAsync(string noteId, string fileRefId, string? noteFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<UpdateVaultNoteFileRefResponse> UpdateNoteFileRefAsync(string noteFileRefId, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null);
    Task<DeleteVaultNoteFileRefResponse> DeleteNoteFileRefAsync(string noteFileRefId);

    Task<CreateVaultFileRefRelationResponse> CreateFileRefRelationAsync(string sourceFileRefId, string targetFileRefId, string relationType, string? description = null, string? fileRefRelationId = null, int? sortOrder = null, float? weight = null);
    Task<UpdateVaultFileRefRelationResponse> UpdateFileRefRelationAsync(string fileRefRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null);
    Task<DeleteVaultFileRefRelationResponse> DeleteFileRefRelationAsync(string fileRefRelationId);

    // ── Note Relations / Metadata / Tags ─────────────────────────────────────
    Task<CreateVaultNoteRelationResponse> CreateNoteRelationAsync(string sourceNoteId, string targetNoteId, string relationType, string? description = null, string? noteRelationId = null, int? sortOrder = null, float? weight = null);
    Task<UpdateVaultNoteRelationResponse> UpdateNoteRelationAsync(string noteRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null);
    Task<DeleteVaultNoteRelationResponse> DeleteNoteRelationAsync(string noteRelationId);

    Task<CreateVaultNoteVaultTagResponse> CreateNoteVaultTagAsync(string noteId, string tagId, string? noteVaultTagId = null, int? sortOrder = null);
    Task<UpdateVaultNoteVaultTagResponse> UpdateNoteVaultTagAsync(string noteVaultTagId, int? sortOrder = null);
    Task<DeleteVaultNoteVaultTagResponse> DeleteNoteVaultTagAsync(string noteVaultTagId);

    Task<UpdateVaultTagResponse> UpdateTagAsync(string tagId, string? name = null, string? description = null, string? colorHex = null, int? sortOrder = null, string? vaultId = null);

    Task<CreateVaultMetadataResponse> CreateMetadataAsync(string noteId, string key, string? value = null, string? metadataId = null);
    Task<UpdateVaultMetadataResponse> UpdateMetadataAsync(string metadataId, string? key = null, string? value = null);
    Task<DeleteVaultMetadataResponse> DeleteMetadataAsync(string metadataId);

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
