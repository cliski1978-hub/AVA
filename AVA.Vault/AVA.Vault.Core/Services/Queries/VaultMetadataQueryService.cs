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
    public sealed class VaultMetadataQueryService : IVaultMetadataQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultMetadataQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultMetadata?> GetByIdAsync(string metadataId, CancellationToken ct = default)
        {
            var result = await _db.VaultMetadata
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.ID == metadataId, ct);

            _logger.Log(nameof(VaultMetadataQueryService), $"Retrieved VaultMetadata [{metadataId}]");
            return result;
        }

        public async Task<List<VaultMetadata>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.VaultMetadata
                .AsNoTracking()
                .Where(m => m.NoteID == noteId)
                .OrderBy(m => m.Key)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultMetadataQueryService), $"Retrieved {results.Count} VaultMetadata for note [{noteId}]");
            return results;
        }

        public async Task<VaultMetadata?> GetByNoteIdAndKeyAsync(string noteId, string key, CancellationToken ct = default)
        {
            var result = await _db.VaultMetadata
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.NoteID == noteId && m.Key == key, ct);

            _logger.Log(nameof(VaultMetadataQueryService), $"Retrieved VaultMetadata for note [{noteId}] key [{key}]");
            return result;
        }

        public async Task<List<VaultMetadata>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default)
        {
            var results = await _db.VaultMetadata
                .AsNoTracking()
                .Where(m => m.OwnerID == ownerId)
                .OrderBy(m => m.Key)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultMetadataQueryService), $"Retrieved {results.Count} VaultMetadata for owner [{ownerId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string metadataId, CancellationToken ct = default)
        {
            var result = await _db.VaultMetadata
                .AnyAsync(m => m.ID == metadataId, ct);

            _logger.Log(nameof(VaultMetadataQueryService), $"VaultMetadata [{metadataId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var result = await _db.VaultMetadata
                .CountAsync(m => m.NoteID == noteId, ct);

            _logger.Log(nameof(VaultMetadataQueryService), $"Counted {result} VaultMetadata for note [{noteId}]");
            return result;
        }
    }
}
