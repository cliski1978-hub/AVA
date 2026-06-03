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
        Task<CreateVaultNoteResponse>    CreateVaultNoteAsync(string vaultId, string? projectId, string title, string content, string? sessionId = null, CancellationToken ct = default);
        Task<VaultNote?>                 GetVaultNoteAsync(string vaultId, string noteId, CancellationToken ct = default);
        Task<UpdateVaultNoteResponse>    UpdateNoteAsync(string vaultId,string noteId, string? title, string? content, CancellationToken ct = default);
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
        // ── Workflow ────────────────────────────────────────────────────────────
        Task<CreateVaultWorkflowResponse> CreateWorkflowAsync(string vaultId, string? projectId, string name, string? description = null, string? workflowType = null, string? status = null, int? sortOrder = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowResponse> UpdateWorkflowAsync(string workflowId, string? name, string? description, string? status, int? sortOrder = null, string? projectId = null, string? workflowType = null, CancellationToken ct = default);
        Task<DeleteVaultWorkflowResponse> DeleteWorkflowAsync(string workflowId, CancellationToken ct = default);

        // ── Workflow Node ───────────────────────────────────────────────────────
        Task<CreateVaultWorkflowNodeResponse> CreateWorkflowNodeAsync(string workflowId, string name, string? workflowNodeId = null, string? description = null, string? instructions = null, string? metadataJson = null, string? nodeType = null, int? nodeOrder = null, string? status = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowNodeResponse> UpdateWorkflowNodeAsync(string workflowNodeId, string? name = null, string? description = null, string? instructions = null, string? nodeType = null, int? nodeOrder = null, string? status = null, string? metadataJson = null, CancellationToken ct = default);
        Task<DeleteVaultWorkflowNodeResponse> DeleteWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);

        // ── Workflow Line ───────────────────────────────────────────────────────
        Task<CreateVaultWorkflowLineResponse> CreateWorkflowLineAsync(string workflowId, string sourceWorkflowNodeId, string targetWorkflowNodeId, string name, string? workflowLineId = null, string? description = null, string? conditionJson = null, bool? isDefaultLine = null, string? lineType = null, int? lineOrder = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowLineResponse> UpdateWorkflowLineAsync(string workflowLineId, string? name = null, string? description = null, string? conditionJson = null, bool? isDefaultLine = null, string? lineType = null, int? lineOrder = null, CancellationToken ct = default);
        Task<DeleteVaultWorkflowLineResponse> DeleteWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);

        // ── Workflow Line Step ──────────────────────────────────────────────────
        Task<CreateVaultWorkflowLineStepResponse> CreateWorkflowLineStepAsync(string workflowLineId, string name, string? workflowLineStepId = null, string? description = null, string? instructions = null, bool? isRequired = null, int? stepOrder = null, string? stepType = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowLineStepResponse> UpdateWorkflowLineStepAsync(string workflowLineStepId, string? name = null, string? description = null, string? instructions = null, bool? isRequired = null, int? stepOrder = null, string? stepType = null, CancellationToken ct = default);
        Task<DeleteVaultWorkflowLineStepResponse> DeleteWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);

        // ── Workflow Note Links ────────────────────────────────────────────────
        Task<CreateVaultWorkflowNoteResponse> CreateWorkflowNoteAsync(string workflowId, string noteId, string? workflowNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowNoteResponse> UpdateWorkflowNoteAsync(string workflowNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultWorkflowNoteResponse> DeleteWorkflowNoteAsync(string workflowNoteId, CancellationToken ct = default);

        Task<CreateVaultWorkflowNodeNoteResponse> CreateWorkflowNodeNoteAsync(string workflowNodeId, string noteId, string? workflowNodeNoteId = null, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowNodeNoteResponse> UpdateWorkflowNodeNoteAsync(string workflowNodeNoteId, string? usageRole, string? instructions, bool? isRequired, int? noteOrder, CancellationToken ct = default);
        Task<DeleteVaultWorkflowNodeNoteResponse> DeleteWorkflowNodeNoteAsync(string workflowNodeNoteId, CancellationToken ct = default);

        Task<CreateVaultWorkflowLineNoteResponse> CreateWorkflowLineNoteAsync(string workflowLineId, string noteId, string? workflowLineNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowLineNoteResponse> UpdateWorkflowLineNoteAsync(string workflowLineNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultWorkflowLineNoteResponse> DeleteWorkflowLineNoteAsync(string workflowLineNoteId, CancellationToken ct = default);

        Task<CreateVaultWorkflowLineStepNoteResponse> CreateWorkflowLineStepNoteAsync(string workflowLineStepId, string noteId, string? workflowLineStepNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowLineStepNoteResponse> UpdateWorkflowLineStepNoteAsync(string workflowLineStepNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultWorkflowLineStepNoteResponse> DeleteWorkflowLineStepNoteAsync(string workflowLineStepNoteId, CancellationToken ct = default);

        // ── Workflow FileRef Links ─────────────────────────────────────────────
        Task<CreateVaultWorkflowFileRefResponse> CreateWorkflowFileRefAsync(string workflowId, string fileRefId, string? workflowFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowFileRefResponse> UpdateWorkflowFileRefAsync(string workflowFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultWorkflowFileRefResponse> DeleteWorkflowFileRefAsync(string workflowFileRefId, CancellationToken ct = default);

        Task<CreateVaultWorkflowNodeFileRefResponse> CreateWorkflowNodeFileRefAsync(string workflowNodeId, string fileRefId, string? workflowNodeFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowNodeFileRefResponse> UpdateWorkflowNodeFileRefAsync(string workflowNodeFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultWorkflowNodeFileRefResponse> DeleteWorkflowNodeFileRefAsync(string workflowNodeFileRefId, CancellationToken ct = default);

        Task<CreateVaultWorkflowLineFileRefResponse> CreateWorkflowLineFileRefAsync(string workflowLineId, string fileRefId, string? workflowLineFileRefId = null, string? instructions = null, bool? isRequired = null, int? fileOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowLineFileRefResponse> UpdateWorkflowLineFileRefAsync(string workflowLineFileRefId, string? usageRole, string? instructions, bool? isRequired, int? fileOrder, CancellationToken ct = default);
        Task<DeleteVaultWorkflowLineFileRefResponse> DeleteWorkflowLineFileRefAsync(string workflowLineFileRefId, CancellationToken ct = default);

        Task<CreateVaultWorkflowLineStepFileRefResponse> CreateWorkflowLineStepFileRefAsync(string workflowLineStepId, string fileRefId, string? workflowLineStepFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultWorkflowLineStepFileRefResponse> UpdateWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultWorkflowLineStepFileRefResponse> DeleteWorkflowLineStepFileRefAsync(string workflowLineStepFileRefId, CancellationToken ct = default);

        // ── Vault / Project / Session Note Links ───────────────────────────────
        Task<CreateVaultHeaderNoteResponse> CreateVaultHeaderNoteAsync(string vaultId, string noteId, string? headerNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultHeaderNoteResponse> UpdateVaultHeaderNoteAsync(string headerNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultHeaderNoteResponse> DeleteVaultHeaderNoteAsync(string headerNoteId, CancellationToken ct = default);

        Task<CreateVaultProjectNoteResponse> CreateVaultProjectNoteAsync(string projectId, string noteId, string? projectNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultProjectNoteResponse> UpdateVaultProjectNoteAsync(string projectNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultProjectNoteResponse> DeleteVaultProjectNoteAsync(string projectNoteId, CancellationToken ct = default);

        Task<CreateVaultSessionNoteResponse> CreateVaultSessionNoteAsync(string sessionId, string noteId, string? sessionNoteId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultSessionNoteResponse> UpdateVaultSessionNoteAsync(string sessionNoteId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultSessionNoteResponse> DeleteVaultSessionNoteAsync(string sessionNoteId, CancellationToken ct = default);

        // ── Vault / Project / Session FileRef Links ────────────────────────────
        Task<CreateVaultHeaderFileRefResponse> CreateVaultHeaderFileRefAsync(string vaultId, string fileRefId, string? headerFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultHeaderFileRefResponse> UpdateVaultHeaderFileRefAsync(string headerFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultHeaderFileRefResponse> DeleteVaultHeaderFileRefAsync(string headerFileRefId, CancellationToken ct = default);

        Task<CreateVaultProjectFileRefResponse> CreateVaultProjectFileRefAsync(string projectId, string fileRefId, string? projectFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultProjectFileRefResponse> UpdateVaultProjectFileRefAsync(string projectFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultProjectFileRefResponse> DeleteVaultProjectFileRefAsync(string projectFileRefId, CancellationToken ct = default);

        Task<CreateVaultSessionFileRefResponse> CreateVaultSessionFileRefAsync(string sessionId, string fileRefId, string? sessionFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultSessionFileRefResponse> UpdateVaultSessionFileRefAsync(string sessionFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultSessionFileRefResponse> DeleteVaultSessionFileRefAsync(string sessionFileRefId, CancellationToken ct = default);

        // ── FileRefs ───────────────────────────────────────────────────────────
        Task<CreateVaultFileRefResponse> CreateFileRefAsync(string vaultId, string name, string path, string? fileRefId = null, string? projectId = null, string? sessionId = null, long? fileSizeBytes = null, string? contentHash = null, string? mimeType = null, int? fileOrder = null, CancellationToken ct = default);
        Task<UpdateVaultFileRefResponse> UpdateFileRefAsync(string fileRefId, string? name = null, string? path = null, string? mimeType = null, string? contentHash = null, long? fileSizeBytes = null, int? fileOrder = null, string? vaultId = null, string? projectId = null, string? sessionId = null, CancellationToken ct = default);
        Task<DeleteVaultFileRefResponse> DeleteFileRefAsync(string fileRefId, CancellationToken ct = default);

        // ── FileRef Note / Note FileRef Links ──────────────────────────────────
        Task<CreateVaultFileRefNoteResponse> CreateFileRefNoteAsync(string fileRefId, string noteId, string? fileRefNoteId = null, string? instructions = null, bool? isRequired = null, int? noteOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultFileRefNoteResponse> UpdateFileRefNoteAsync(string fileRefNoteId, string? usageRole, string? instructions, bool? isRequired, int? noteOrder, CancellationToken ct = default);
        Task<DeleteVaultFileRefNoteResponse> DeleteFileRefNoteAsync(string fileRefNoteId, CancellationToken ct = default);

        Task<CreateVaultNoteFileRefResponse> CreateNoteFileRefAsync(string noteId, string fileRefId, string? noteFileRefId = null, string? instructions = null, bool? isRequired = null, int? sortOrder = null, string? usageRole = null, CancellationToken ct = default);
        Task<UpdateVaultNoteFileRefResponse> UpdateNoteFileRefAsync(string noteFileRefId, string? usageRole, string? instructions, bool? isRequired, int? sortOrder, CancellationToken ct = default);
        Task<DeleteVaultNoteFileRefResponse> DeleteNoteFileRefAsync(string noteFileRefId, CancellationToken ct = default);

        // ── FileRef Relations ──────────────────────────────────────────────────
        Task<CreateVaultFileRefRelationResponse> CreateFileRefRelationAsync(string sourceFileRefId, string targetFileRefId, string relationType, string? description = null, string? fileRefRelationId = null, int? sortOrder = null, float? weight = null, CancellationToken ct = default);
        Task<UpdateVaultFileRefRelationResponse> UpdateFileRefRelationAsync(string fileRefRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null, CancellationToken ct = default);
        Task<DeleteVaultFileRefRelationResponse> DeleteFileRefRelationAsync(string fileRefRelationId, CancellationToken ct = default);

        // ── Note Relations / Tags / Metadata ───────────────────────────────────
        Task<CreateVaultNoteRelationResponse> CreateNoteRelationAsync(string sourceNoteId, string targetNoteId, string relationType, string? description = null, string? noteRelationId = null, int? sortOrder = null, float? weight = null, CancellationToken ct = default);
        Task<UpdateVaultNoteRelationResponse> UpdateNoteRelationAsync(string noteRelationId, string? relationType = null, string? description = null, int? sortOrder = null, float? weight = null, CancellationToken ct = default);
        Task<DeleteVaultNoteRelationResponse> DeleteNoteRelationAsync(string noteRelationId, CancellationToken ct = default);

        Task<CreateVaultNoteVaultTagResponse> CreateNoteVaultTagAsync(string noteId, string tagId, string? noteVaultTagId = null, int? sortOrder = null, CancellationToken ct = default);
        Task<UpdateVaultNoteVaultTagResponse> UpdateNoteVaultTagAsync(string noteVaultTagId, int? sortOrder = null, CancellationToken ct = default);
        Task<DeleteVaultNoteVaultTagResponse> DeleteNoteVaultTagAsync(string noteVaultTagId, CancellationToken ct = default);

        Task<UpdateVaultTagResponse> UpdateTagAsync(string tagId, string? name = null, string? description = null, string? colorHex = null, int? sortOrder = null, string? vaultId = null, CancellationToken ct = default);

        Task<CreateVaultMetadataResponse> CreateMetadataAsync(string noteId, string key, string? value = null, string? metadataId = null, CancellationToken ct = default);
        Task<UpdateVaultMetadataResponse> UpdateMetadataAsync(string metadataId, string? key = null, string? value = null, CancellationToken ct = default);
        Task<DeleteVaultMetadataResponse> DeleteMetadataAsync(string metadataId, CancellationToken ct = default);
    }
}
