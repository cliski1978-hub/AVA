using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineStepQueryService
    {
        Task<VaultWorkflowLineStep?> GetByIdAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineStep>> GetByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineStep>> GetRequiredByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineStep>> GetByStepTypeAsync(string workflowLineId, string stepType, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<int> CountByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default);
    }
}
