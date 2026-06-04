using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultMetadataQueryService
    {
        Task<VaultMetadata?> GetByIdAsync(string metadataId, CancellationToken ct = default);
        Task<List<VaultMetadata>> GetByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<VaultMetadata?> GetByNoteIdAndKeyAsync(string noteId, string key, CancellationToken ct = default);
        Task<List<VaultMetadata>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default);
        Task<bool> ExistsAsync(string metadataId, CancellationToken ct = default);
        Task<int> CountByNoteIdAsync(string noteId, CancellationToken ct = default);
    }
}
