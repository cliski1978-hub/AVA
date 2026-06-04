using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultSessionQueryService
    {
        Task<VaultSession?> GetByIdAsync(string sessionId, CancellationToken ct = default);
        Task<List<VaultSession>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultSession>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<List<VaultSession>> GetActiveByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultSession>> GetPinnedByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultSession>> GetTemplatesByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultSession>> GetRecentByVaultIdAsync(string vaultId, int take, CancellationToken ct = default);
        Task<bool> ExistsAsync(string sessionId, CancellationToken ct = default);
        Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default);
    }
}
