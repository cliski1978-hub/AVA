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
    public sealed class VaultNoteFileRefQueryService : IVaultNoteFileRefQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultNoteFileRefQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultNoteFileRef?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultNoteFileRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultNoteFileRefQueryService), $"Retrieved VaultNoteFileRef [{id}]");
            return result;
        }

        public async Task<List<VaultNoteFileRef>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultNoteFileRef>()
                .AsNoTracking()
                .Where(x => x.NoteID == noteId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteFileRefQueryService), $"Retrieved {results.Count} VaultNoteFileRefs for note [{noteId}]");
            return results;
        }

        public async Task<List<VaultNoteFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultNoteFileRef>()
                .AsNoTracking()
                .Where(x => x.FileRefID == fileRefId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteFileRefQueryService), $"Retrieved {results.Count} VaultNoteFileRefs for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string noteId, string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultNoteFileRef>()
                .AnyAsync(x => x.NoteID == noteId && x.FileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultNoteFileRefQueryService), $"VaultNoteFileRef for note [{noteId}] fileRef [{fileRefId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultNoteFileRef>()
                .CountAsync(x => x.NoteID == noteId, ct);

            _logger.Log(nameof(VaultNoteFileRefQueryService), $"Counted {result} VaultNoteFileRefs for note [{noteId}]");
            return result;
        }
    }
}
