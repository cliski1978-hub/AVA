using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultNoteVaultTagQueryService
    {
        Task<VaultNoteVaultTag?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultNoteVaultTag>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNoteVaultTag>> GetByTagIdAsync(string tagId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string noteId, string tagId, CancellationToken ct = default);
        Task<int> CountByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<int> CountByTagIdAsync(string tagId, CancellationToken ct = default);
    }
}
