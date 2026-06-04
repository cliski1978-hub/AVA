using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Dtos.Workflows;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowNodeDetailsReadService
    {
        Task<VaultWorkflowNodeDetailsDto?> GetWorkflowNodeDetailsAsync(string workflowNodeId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetWorkflowNodeNotesAsync(string workflowNodeId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetWorkflowNodeFilesAsync(string workflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineDto>> GetWorkflowNodeIncomingLinesAsync(string workflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineDto>> GetWorkflowNodeOutgoingLinesAsync(string workflowNodeId, CancellationToken ct = default);
    }
}
