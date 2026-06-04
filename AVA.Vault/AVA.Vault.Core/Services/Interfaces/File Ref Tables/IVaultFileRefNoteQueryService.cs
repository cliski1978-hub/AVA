using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultFileRefNoteQueryService
    {
        Task<VaultFileRefNote?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultFileRefNote>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<List<VaultFileRefNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string fileRefId, string noteId, CancellationToken ct = default);
        Task<int> CountByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
    }
}
