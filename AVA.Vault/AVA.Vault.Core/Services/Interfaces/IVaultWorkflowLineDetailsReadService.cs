using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Dtos.Workflows;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineDetailsReadService
    {
        Task<VaultWorkflowLineDetailsDto?> GetWorkflowLineDetailsAsync(string workflowLineId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetWorkflowLineNotesAsync(string workflowLineId, CancellationToken ct = default);
        Task<VaultAttachedFilesResponse> GetWorkflowLineFilesAsync(string workflowLineId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineStepDto>> GetWorkflowLineStepsAsync(string workflowLineId, CancellationToken ct = default);
    }
}
