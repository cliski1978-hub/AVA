using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Dtos.Workflows;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineStepDetailsReadService
    {
        Task<VaultWorkflowLineStepDetailsDto?> GetWorkflowLineStepDetailsAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetWorkflowLineStepNotesAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetWorkflowLineStepFilesAsync(string workflowLineStepId, CancellationToken ct = default);
    }
}
