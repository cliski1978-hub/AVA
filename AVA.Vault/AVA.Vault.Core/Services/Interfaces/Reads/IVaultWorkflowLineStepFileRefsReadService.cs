using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineStepFileRefsReadService
    {
        Task<VaultAttachedFilesResponse> GetFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetRequiredFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetOptionalFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<VaultAttachedFileDto?> GetFileForWorkflowLineStepAsync(string workflowLineStepId, string fileRefId, CancellationToken ct = default);
        Task<int> CountFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);
    }
}
