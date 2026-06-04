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
    public sealed class VaultHeaderNoteQueryService : IVaultHeaderNoteQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultHeaderNoteQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultHeaderNote?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultHeaderNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultHeaderNoteQueryService), $"Retrieved VaultHeaderNote [{id}]");
            return result;
        }

        public async Task<List<VaultHeaderNote>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultHeaderNote>()
                .AsNoTracking()
                .Where(x => x.VaultID == vaultId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderNoteQueryService), $"Retrieved {results.Count} VaultHeaderNotes for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultHeaderNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultHeaderNote>()
                .AsNoTracking()
                .Where(x => x.NoteID == noteId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderNoteQueryService), $"Retrieved {results.Count} VaultHeaderNotes for note [{noteId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string vaultId, string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultHeaderNote>()
                .AnyAsync(x => x.VaultID == vaultId && x.NoteID == noteId, ct);

            _logger.Log(nameof(VaultHeaderNoteQueryService), $"VaultHeaderNote for vault [{vaultId}] note [{noteId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultHeaderNote>()
                .CountAsync(x => x.VaultID == vaultId, ct);

            _logger.Log(nameof(VaultHeaderNoteQueryService), $"Counted {result} VaultHeaderNotes for vault [{vaultId}]");
            return result;
        }
    }
}
