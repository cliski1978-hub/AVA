using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowFileRefsReadService
    {
        Task<VaultAttachedFilesResponse> GetFilesForWorkflowAsync(string workflowId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetRequiredFilesForWorkflowAsync(string workflowId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetOptionalFilesForWorkflowAsync(string workflowId, CancellationToken ct = default);
        Task<VaultAttachedFileDto?> GetFileForWorkflowAsync(string workflowId, string fileRefId, CancellationToken ct = default);
        Task<int> CountFilesForWorkflowAsync(string workflowId, CancellationToken ct = default);
    }
}
