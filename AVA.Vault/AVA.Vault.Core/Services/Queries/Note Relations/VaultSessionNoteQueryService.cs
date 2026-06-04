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
    public sealed class VaultSessionNoteQueryService : IVaultSessionNoteQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultSessionNoteQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultSessionNote?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultSessionNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultSessionNoteQueryService), $"Retrieved VaultSessionNote [{id}]");
            return result;
        }

        public async Task<List<VaultSessionNote>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultSessionNote>()
                .AsNoTracking()
                .Where(x => x.SessionID == sessionId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionNoteQueryService), $"Retrieved {results.Count} VaultSessionNotes for session [{sessionId}]");
            return results;
        }

        public async Task<List<VaultSessionNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultSessionNote>()
                .AsNoTracking()
                .Where(x => x.NoteID == noteId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionNoteQueryService), $"Retrieved {results.Count} VaultSessionNotes for note [{noteId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string sessionId, string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultSessionNote>()
                .AnyAsync(x => x.SessionID == sessionId && x.NoteID == noteId, ct);

            _logger.Log(nameof(VaultSessionNoteQueryService), $"VaultSessionNote for session [{sessionId}] note [{noteId}] exists: {result}");
            return result;
        }

        public async Task<int> CountBySessionIdAsync(string sessionId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultSessionNote>()
                .CountAsync(x => x.SessionID == sessionId, ct);

            _logger.Log(nameof(VaultSessionNoteQueryService), $"Counted {result} VaultSessionNotes for session [{sessionId}]");
            return result;
        }
    }
}
