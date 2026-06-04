using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineStepFileRefQueryService
    {
        Task<VaultWorkflowLineStepFileRef?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultWorkflowLineStepFileRef>> GetByWorkflowLineStepIdAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineStepFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowLineStepId, string fileRefId, CancellationToken ct = default);
        Task<int> CountByWorkflowLineStepIdAsync(string workflowLineStepId, CancellationToken ct = default);
    }
}
