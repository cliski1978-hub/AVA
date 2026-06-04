using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultProjectNoteQueryService
    {
        Task<VaultProjectNote?> GetByIdAsync(string id, CancellationToken ct = default);
        Task<List<VaultProjectNote>> GetByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<List<VaultProjectNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNote>> GetNotesByProjectIdAsync(string projectId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string projectId, string noteId, CancellationToken ct = default);
        Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default);
    }
}
