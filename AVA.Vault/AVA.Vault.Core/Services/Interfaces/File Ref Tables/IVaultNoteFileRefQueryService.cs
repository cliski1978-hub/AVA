using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultNoteFileRefQueryService
    {
        Task<VaultNoteFileRef?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultNoteFileRef>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNoteFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string noteId, string fileRefId, CancellationToken ct = default);
        Task<int> CountByNoteIdAsync(string noteId, CancellationToken ct = default);
    }
}
