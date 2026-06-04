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
    public sealed class VaultNoteRelationQueryService : IVaultNoteRelationQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultNoteRelationQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultNoteRelation?> GetByIdAsync(string relationId, CancellationToken ct = default)
        {
            var result = await _db.VaultNoteRelations
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ID == relationId, ct);

            _logger.Log(nameof(VaultNoteRelationQueryService), $"Retrieved VaultNoteRelation [{relationId}]");
            return result;
        }

        public async Task<List<VaultNoteRelation>> GetOutgoingByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.VaultNoteRelations
                .AsNoTracking()
                .Where(r => r.SourceNoteID == noteId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteRelationQueryService), $"Retrieved {results.Count} outgoing VaultNoteRelations from note [{noteId}]");
            return results;
        }

        public async Task<List<VaultNoteRelation>> GetIncomingByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.VaultNoteRelations
                .AsNoTracking()
                .Where(r => r.TargetNoteID == noteId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteRelationQueryService), $"Retrieved {results.Count} incoming VaultNoteRelations to note [{noteId}]");
            return results;
        }

        public async Task<List<VaultNoteRelation>> GetAllByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.VaultNoteRelations
                .AsNoTracking()
                .Where(r => r.SourceNoteID == noteId || r.TargetNoteID == noteId)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteRelationQueryService), $"Retrieved {results.Count} total VaultNoteRelations for note [{noteId}]");
            return results;
        }

        public async Task<List<VaultNoteRelation>> GetByRelationTypeAsync(string noteId, string relationType, CancellationToken ct = default)
        {
            var results = await _db.VaultNoteRelations
                .AsNoTracking()
                .Where(r => (r.SourceNoteID == noteId || r.TargetNoteID == noteId) && r.RelationType == relationType)
                .OrderBy(r => r.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteRelationQueryService), $"Retrieved {results.Count} VaultNoteRelations for note [{noteId}] of type [{relationType}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string relationId, CancellationToken ct = default)
        {
            var result = await _db.VaultNoteRelations
                .AnyAsync(r => r.ID == relationId, ct);

            _logger.Log(nameof(VaultNoteRelationQueryService), $"VaultNoteRelation [{relationId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var result = await _db.VaultNoteRelations
                .CountAsync(r => r.SourceNoteID == noteId || r.TargetNoteID == noteId, ct);

            _logger.Log(nameof(VaultNoteRelationQueryService), $"Counted {result} VaultNoteRelations for note [{noteId}]");
            return result;
        }
    }
}
