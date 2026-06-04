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
    public sealed class VaultFileRefNoteQueryService : IVaultFileRefNoteQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultFileRefNoteQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultFileRefNote?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultFileRefNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultFileRefNoteQueryService), $"Retrieved VaultFileRefNote [{id}]");
            return result;
        }

        public async Task<List<VaultFileRefNote>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultFileRefNote>()
                .AsNoTracking()
                .Where(x => x.FileRefID == fileRefId)
                .OrderBy(x => x.NoteOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefNoteQueryService), $"Retrieved {results.Count} VaultFileRefNotes for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<List<VaultFileRefNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultFileRefNote>()
                .AsNoTracking()
                .Where(x => x.NoteID == noteId)
                .OrderBy(x => x.NoteOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefNoteQueryService), $"Retrieved {results.Count} VaultFileRefNotes for note [{noteId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string fileRefId, string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultFileRefNote>()
                .AnyAsync(x => x.FileRefID == fileRefId && x.NoteID == noteId, ct);

            _logger.Log(nameof(VaultFileRefNoteQueryService), $"VaultFileRefNote for fileRef [{fileRefId}] note [{noteId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultFileRefNote>()
                .CountAsync(x => x.FileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultFileRefNoteQueryService), $"Counted {result} VaultFileRefNotes for fileRef [{fileRefId}]");
            return result;
        }
    }
}
