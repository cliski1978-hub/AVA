using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowNodeQueryService
    {
        Task<VaultWorkflowNode?> GetByIdAsync(string workflowNodeId, CancellationToken ct = default);
        Task<List<VaultWorkflowNode>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
        Task<List<VaultWorkflowNode>> GetByWorkflowIdAndTypeAsync(string workflowId, string nodeType, CancellationToken ct = default);
        Task<List<VaultWorkflowNode>> GetByStatusAsync(string workflowId, string status, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowNodeId, CancellationToken ct = default);
        Task<int> CountByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
    }
}
