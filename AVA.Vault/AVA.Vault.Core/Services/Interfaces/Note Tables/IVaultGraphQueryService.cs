using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultGraphQueryService
    {
        Task<VaultGraph?> GetByIdAsync(string graphId, CancellationToken ct = default);
        Task<List<VaultGraph>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<VaultGraph?> GetLatestByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string graphId, CancellationToken ct = default);
        Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default);
    }
}
