using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultHeaderFileRefQueryService
    {
        Task<VaultHeaderFileRef?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultHeaderFileRef>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultHeaderFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string vaultId, string fileRefId, CancellationToken ct = default);
        Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default);
    }
}
