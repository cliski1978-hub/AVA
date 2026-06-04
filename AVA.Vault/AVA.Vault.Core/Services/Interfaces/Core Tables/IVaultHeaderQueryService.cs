using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultHeaderQueryService
    {
        Task<VaultHeader?> GetByIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultHeader>> GetAllAsync(CancellationToken ct = default);
        Task<List<VaultHeader>> GetActiveAsync(CancellationToken ct = default);
        Task<List<VaultHeader>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default);
        Task<List<VaultHeader>> GetActiveByOwnerIdAsync(string ownerId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string vaultId, CancellationToken ct = default);
        Task<int> CountAsync(CancellationToken ct = default);
        Task<int> CountActiveAsync(CancellationToken ct = default);
        Task<int> CountByOwnerIdAsync(string ownerId, CancellationToken ct = default);
        Task<List<VaultHeader>> SearchAsync(string searchText, CancellationToken ct = default);
        Task<List<VaultHeader>> SearchActiveAsync(string searchText, CancellationToken ct = default);
    }
}
