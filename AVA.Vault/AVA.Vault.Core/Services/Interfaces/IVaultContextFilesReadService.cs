using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Files;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultContextFilesReadService
    {
        Task<VaultContextFilesDto> GetFilesForVaultAsync(string vaultId, CancellationToken ct = default);
        Task<VaultContextFilesDto> GetFilesForProjectAsync(string projectId, CancellationToken ct = default);
        Task<VaultContextFilesDto> GetFilesForSessionAsync(string sessionId, CancellationToken ct = default);
        Task<VaultContextFilesDto> GetFilesForNoteAsync(string noteId, CancellationToken ct = default);
        Task<VaultContextFilesDto> GetFilesForWorkflowAsync(string workflowId, CancellationToken ct = default);
        Task<VaultContextFilesDto> GetFilesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);
        Task<VaultContextFilesDto> GetFilesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);
        Task<VaultContextFilesDto> GetFilesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);
    }
}
