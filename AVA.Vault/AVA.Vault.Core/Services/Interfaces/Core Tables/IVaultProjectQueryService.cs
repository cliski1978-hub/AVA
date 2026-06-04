using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultProjectQueryService
    {
        Task<VaultProject?> GetByIdAsync(string projectId, CancellationToken ct = default);
        Task<List<VaultProject>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultProject>> GetActiveByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultProject>> GetArchivedByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string projectId, CancellationToken ct = default);
        Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultProject>> SearchByVaultIdAsync(string vaultId, string searchText, CancellationToken ct = default);
    }
}
