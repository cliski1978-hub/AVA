using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowFileRefQueryService
    {
        Task<VaultWorkflowFileRef?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultWorkflowFileRef>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
        Task<List<VaultWorkflowFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowId, string fileRefId, CancellationToken ct = default);
        Task<int> CountByWorkflowIdAsync(string workflowId, CancellationToken ct = default);
    }
}
