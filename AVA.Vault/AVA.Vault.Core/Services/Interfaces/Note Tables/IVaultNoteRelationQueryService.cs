using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultNoteRelationQueryService
    {
        Task<VaultNoteRelation?> GetByIdAsync(string relationId, CancellationToken ct = default);
        Task<List<VaultNoteRelation>> GetOutgoingByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNoteRelation>> GetIncomingByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNoteRelation>> GetAllByNoteIdAsync(string noteId, CancellationToken ct = default);
        Task<List<VaultNoteRelation>> GetByRelationTypeAsync(string noteId, string relationType, CancellationToken ct = default);
        Task<bool> ExistsAsync(string relationId, CancellationToken ct = default);
        Task<int> CountByNoteIdAsync(string noteId, CancellationToken ct = default);
    }
}
