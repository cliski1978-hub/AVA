using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowNodeFileRefsReadService
    {
        Task<VaultAttachedFilesResponse> GetFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetRequiredFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetOptionalFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);
        Task<VaultAttachedFileDto?> GetFileForWorkflowNodeAsync(string workflowNodeId, string fileRefId, CancellationToken ct = default);
        Task<int> CountFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);
    }
}
