using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Queries
{
    public sealed class VaultFileRefRelationQueryService : IVaultFileRefRelationQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultFileRefRelationQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultFileRefRelation?> GetByIdAsync(string relationId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultFileRefRelation>()
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ID == relationId, ct);

            _logger.Log(nameof(VaultFileRefRelationQueryService), $"Retrieved VaultFileRefRelation [{relationId}]");
            return result;
        }

        public async Task<List<VaultFileRefRelation>> GetOutgoingByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultFileRefRelation>()
                .AsNoTracking()
                .Where(r => r.SourceFileRefID == fileRefId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefRelationQueryService), $"Retrieved {results.Count} outgoing VaultFileRefRelations from fileRef [{fileRefId}]");
            return results;
        }

        public async Task<List<VaultFileRefRelation>> GetIncomingByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultFileRefRelation>()
                .AsNoTracking()
                .Where(r => r.TargetFileRefID == fileRefId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefRelationQueryService), $"Retrieved {results.Count} incoming VaultFileRefRelations to fileRef [{fileRefId}]");
            return results;
        }

        public async Task<List<VaultFileRefRelation>> GetAllByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultFileRefRelation>()
                .AsNoTracking()
                .Where(r => r.SourceFileRefID == fileRefId || r.TargetFileRefID == fileRefId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefRelationQueryService), $"Retrieved {results.Count} total VaultFileRefRelations for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<List<VaultFileRefRelation>> GetByRelationTypeAsync(string fileRefId, string relationType, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultFileRefRelation>()
                .AsNoTracking()
                .Where(r => (r.SourceFileRefID == fileRefId || r.TargetFileRefID == fileRefId) && r.RelationType == relationType)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefRelationQueryService), $"Retrieved {results.Count} VaultFileRefRelations for fileRef [{fileRefId}] of type [{relationType}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string relationId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultFileRefRelation>()
                .AnyAsync(r => r.ID == relationId, ct);

            _logger.Log(nameof(VaultFileRefRelationQueryService), $"VaultFileRefRelation [{relationId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultFileRefRelation>()
                .CountAsync(r => r.SourceFileRefID == fileRefId || r.TargetFileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultFileRefRelationQueryService), $"Counted {result} VaultFileRefRelations for fileRef [{fileRefId}]");
            return result;
        }
    }
}
