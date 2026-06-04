using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Workflows;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowDetailsReadService
    {
        Task<VaultWorkflowDetailsDto?> GetWorkflowDetailsAsync(string workflowId, CancellationToken ct = default);
        Task<VaultWorkflowSummaryDto?> GetWorkflowSummaryAsync(string workflowId, CancellationToken ct = default);
    }
}
