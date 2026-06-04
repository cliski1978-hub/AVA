using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultFileRefRelationQueryService
    {
        Task<VaultFileRefRelation?> GetByIdAsync(string relationId, CancellationToken ct = default);
        Task<List<VaultFileRefRelation>> GetOutgoingByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<List<VaultFileRefRelation>> GetIncomingByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<List<VaultFileRefRelation>> GetAllByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
        Task<List<VaultFileRefRelation>> GetByRelationTypeAsync(string fileRefId, string relationType, CancellationToken ct = default);
        Task<bool> ExistsAsync(string relationId, CancellationToken ct = default);
        Task<int> CountByFileRefIdAsync(string fileRefId, CancellationToken ct = default);
    }
}
