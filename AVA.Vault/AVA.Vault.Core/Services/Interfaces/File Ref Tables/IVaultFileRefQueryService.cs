using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultFileRefQueryService
    {
        Task<VaultFileRef?> GetByIdAsync(string fileRefId, CancellationToken ct = default);
        Task<List<VaultFileRef>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultFileRef>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<List<VaultFileRef>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
        Task<List<VaultFileRef>> SearchByVaultIdAsync(string vaultId, string searchText, CancellationToken ct = default);
        Task<bool> ExistsAsync(string fileRefId, CancellationToken ct = default);
        Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultFileRef>> GetByIdsAsync(List<string> fileRefIds, CancellationToken ct = default);
    }
}
