using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultSessionNoteQueryService
    {
        Task<VaultSessionNote?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultSessionNote>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default);
        Task<List<VaultSessionNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string sessionId, string noteId, CancellationToken ct = default);
        Task<int> CountBySessionIdAsync(string sessionId, CancellationToken ct = default);
    }
}
