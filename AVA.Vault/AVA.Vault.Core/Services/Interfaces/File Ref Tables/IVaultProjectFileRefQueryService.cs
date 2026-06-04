using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultProjectFileRefQueryService
    {
        Task<VaultProjectFileRef?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultProjectFileRef>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<List<VaultProjectFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string projectId, string fileRefId, CancellationToken ct = default);
        Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default);
    }
}
