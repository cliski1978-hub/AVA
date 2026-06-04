using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultTagQueryService
    {
        Task<VaultTag?> GetByIdAsync(string tagId, CancellationToken ct = default);
        Task<List<VaultTag>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<VaultTag?> GetByNameAsync(string projectId, string name, CancellationToken ct = default);
        Task<List<VaultTag>> SearchByProjectIdAsync(string projectId, string searchText, CancellationToken ct = default);
        Task<bool> ExistsAsync(string tagId, CancellationToken ct = default);
        Task<bool> ExistsByNameAsync(string projectId, string name, CancellationToken ct = default);
        Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<List<VaultTag>> GetByIdsAsync(List<string> tagIds, CancellationToken ct = default);
    }
}
