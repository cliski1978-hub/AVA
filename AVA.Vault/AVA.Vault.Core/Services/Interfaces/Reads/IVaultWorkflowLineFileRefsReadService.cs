using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineFileRefsReadService
    {
        Task<VaultAttachedFilesResponse> GetFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetRequiredFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetOptionalFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);
        Task<VaultAttachedFileDto?> GetFileForWorkflowLineAsync(string workflowLineId, string fileRefId, CancellationToken ct = default);
        Task<int> CountFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);
    }
}
