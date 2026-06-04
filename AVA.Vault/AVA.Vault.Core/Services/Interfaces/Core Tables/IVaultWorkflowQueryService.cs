using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowQueryService
    {
        Task<VaultWorkflow?> GetByIdAsync(string workflowId, CancellationToken ct = default);
        Task<List<VaultWorkflow>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<List<VaultWorkflow>> GetByStatusAsync(string projectId, string status, CancellationToken ct = default);
        Task<List<VaultWorkflow>> GetByWorkflowTypeAsync(string projectId, string workflowType, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowId, CancellationToken ct = default);
        Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<List<VaultWorkflow>> SearchByProjectIdAsync(string projectId, string searchText, CancellationToken ct = default);
    }
}
