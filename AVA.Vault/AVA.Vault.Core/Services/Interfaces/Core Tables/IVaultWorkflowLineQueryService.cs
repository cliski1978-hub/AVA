using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineQueryService
    {
        Task<VaultWorkflowLine?> GetByIdAsync(string workflowLineId, CancellationToken ct = default);
        Task<List<VaultWorkflowLine>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
        Task<List<VaultWorkflowLine>> GetBySourceWorkflowNodeIdAsync(string sourceWorkflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowLine>> GetByTargetWorkflowNodeIdAsync(string targetWorkflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowLine>> GetOutgoingLinesAsync(string workflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowLine>> GetIncomingLinesAsync(string workflowNodeId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowLineId, CancellationToken ct = default);
        Task<int> CountByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
    }
}
