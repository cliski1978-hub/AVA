using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowNodeFileRefQueryService
    {
        Task<VaultWorkflowNodeFileRef?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultWorkflowNodeFileRef>> GetByWorkflowNodeIdAsync(string workflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowNodeFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowNodeId, string fileRefId, CancellationToken ct = default);
        Task<int> CountByWorkflowNodeIdAsync(string workflowNodeId, CancellationToken ct = default);
    }
}
