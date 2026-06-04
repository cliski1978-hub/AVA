using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultHeaderNoteQueryService
    {
        Task<VaultHeaderNote?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultHeaderNote>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultHeaderNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string vaultId, string noteId, CancellationToken ct = default);
        Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default);
    }
}
