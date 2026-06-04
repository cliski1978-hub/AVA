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
    public sealed class VaultNoteQueryService : IVaultNoteQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultNoteQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultNote?> GetByIdAsync(string noteId, CancellationToken ct = default)
        {
            var result = await _db.VaultNotes
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.ID == noteId, ct);

            _logger.Log(nameof(VaultNoteQueryService), $"Retrieved VaultNote [{noteId}]");
            return result;
        }

        public async Task<List<VaultNote>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultNotes
                .AsNoTracking()
                .Where(n => n.VaultID == vaultId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteQueryService), $"Retrieved {results.Count} VaultNotes for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultNote>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default)
        {
            var results = await _db.VaultNotes
                .AsNoTracking()
                .Where(n => n.SessionID == sessionId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteQueryService), $"Retrieved {results.Count} VaultNotes for session [{sessionId}]");
            return results;
        }

        public async Task<List<VaultNote>> GetPinnedByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultNotes
                .AsNoTracking()
                .Where(n => n.VaultID == vaultId && n.IsPinned)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteQueryService), $"Retrieved {results.Count} pinned VaultNotes for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultNote>> GetTemplatesByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultNotes
                .AsNoTracking()
                .Where(n => n.VaultID == vaultId && n.IsTemplate)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteQueryService), $"Retrieved {results.Count} template VaultNotes for vault [{vaultId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string noteId, CancellationToken ct = default)
        {
            var result = await _db.VaultNotes
                .AnyAsync(n => n.ID == noteId, ct);

            _logger.Log(nameof(VaultNoteQueryService), $"Checked existence of VaultNote [{noteId}]: {result}");
            return result;
        }

        public async Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var result = await _db.VaultNotes
                .CountAsync(n => n.VaultID == vaultId, ct);

            _logger.Log(nameof(VaultNoteQueryService), $"Counted {result} VaultNotes for vault [{vaultId}]");
            return result;
        }

        public async Task<List<VaultNote>> SearchByVaultIdAsync(string vaultId, string searchText, CancellationToken ct = default)
        {
            var results = await _db.VaultNotes
                .AsNoTracking()
                .Where(n => n.VaultID == vaultId && (n.Title.Contains(searchText) || (n.Content != null && n.Content.Contains(searchText))))
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteQueryService), $"Searched VaultNotes in vault [{vaultId}] with text [{searchText}], found {results.Count} results");
            return results;
        }

        public async Task<List<VaultNote>> GetByIdsAsync(List<string> noteIds, CancellationToken ct = default)
        {
            if (noteIds == null || noteIds.Count == 0)
                return new List<VaultNote>();

            var results = await _db.VaultNotes
                .AsNoTracking()
                .Where(n => noteIds.Contains(n.ID))
                .ToListAsync(ct);

            _logger.Log(nameof(VaultNoteQueryService), $"Retrieved {results.Count} VaultNotes by {noteIds.Count} IDs");
            return results;
        }
    }
}
