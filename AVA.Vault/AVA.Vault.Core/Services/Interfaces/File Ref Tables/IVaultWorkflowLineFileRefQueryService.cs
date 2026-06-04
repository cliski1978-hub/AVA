using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineFileRefQueryService
    {
        Task<VaultWorkflowLineFileRef?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultWorkflowLineFileRef>> GetByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default);
        Task<List<VaultWorkflowLineFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string workflowLineId, string fileRefId, CancellationToken ct = default);
        Task<int> CountByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default);
    }
}
