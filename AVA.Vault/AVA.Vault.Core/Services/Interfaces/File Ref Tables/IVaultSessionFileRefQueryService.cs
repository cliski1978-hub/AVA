using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultSessionFileRefQueryService
    {
        Task<VaultSessionFileRef?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultSessionFileRef>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
        Task<List<VaultSessionFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string sessionId, string fileRefId, CancellationToken ct = default);
        Task<int> CountBySessionIdAsync(string sessionId, CancellationToken ct = default);
    }
}
