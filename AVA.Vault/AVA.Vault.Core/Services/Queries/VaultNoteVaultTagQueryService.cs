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
    public sealed class VaultNoteVaultTagQueryService : IVaultNoteVaultTagQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultNoteVaultTagQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultNoteVaultTag?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultNoteVaultTag>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultNoteVaultTagQueryService), $"Retrieved VaultNoteVaultTag [{id}]");
            return result;
        }

        public async Task<List<VaultNoteVaultTag>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultNoteVaultTag>()
                .AsNoTracking()
                .Where(x => x.NoteID == noteId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteVaultTagQueryService), $"Retrieved {results.Count} VaultNoteVaultTags for note [{noteId}]");
            return results;
        }

        public async Task<List<VaultNoteVaultTag>> GetByTagIdAsync(string tagId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultNoteVaultTag>()
                .AsNoTracking()
                .Where(x => x.TagID == tagId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteVaultTagQueryService), $"Retrieved {results.Count} VaultNoteVaultTags for tag [{tagId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string noteId, string tagId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultNoteVaultTag>()
                .AnyAsync(x => x.NoteID == noteId && x.TagID == tagId, ct);

            _logger.Log(nameof(VaultNoteVaultTagQueryService), $"VaultNoteVaultTag for note [{noteId}] tag [{tagId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultNoteVaultTag>()
                .CountAsync(x => x.NoteID == noteId, ct);

            _logger.Log(nameof(VaultNoteVaultTagQueryService), $"Counted {result} VaultNoteVaultTags for note [{noteId}]");
            return result;
        }

        public async Task<int> CountByTagIdAsync(string tagId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultNoteVaultTag>()
                .CountAsync(x => x.TagID == tagId, ct);

            _logger.Log(nameof(VaultNoteVaultTagQueryService), $"Counted {result} VaultNoteVaultTags for tag [{tagId}]");
            return result;
        }
    }
}
