using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultNoteQueryService
    {
        Task<VaultNote?> GetByIdAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNote>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultNote>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
        Task<List<VaultNote>> GetPinnedByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultNote>> GetTemplatesByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string noteId, CancellationToken ct = default);
        Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default);
        Task<List<VaultNote>> SearchByVaultIdAsync(string vaultId, string searchText, CancellationToken ct = default);
        Task<List<VaultNote>> GetByIdsAsync(List<string> noteIds, CancellationToken ct = default);
    }
}
