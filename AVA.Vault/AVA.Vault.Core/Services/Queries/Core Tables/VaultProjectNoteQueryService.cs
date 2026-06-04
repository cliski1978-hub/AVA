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
    public sealed class VaultProjectNoteQueryService : IVaultProjectNoteQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultProjectNoteQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultProjectNote?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultProjectNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(pn => pn.ID == id, ct);

            _logger.Log(nameof(VaultProjectNoteQueryService), $"Retrieved VaultProjectNote [{id}]");
            return result;
        }

        public async Task<List<VaultProjectNote>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultProjectNote>()
                .AsNoTracking()
                .Where(pn => pn.ProjectID == projectId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultProjectNoteQueryService), $"Retrieved {results.Count} VaultProjectNotes for project [{projectId}]");
            return results;
        }

        public async Task<List<VaultProjectNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultProjectNote>()
                .AsNoTracking()
                .Where(pn => pn.NoteID == noteId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultProjectNoteQueryService), $"Retrieved {results.Count} VaultProjectNotes for note [{noteId}]");
            return results;
        }

        public async Task<List<VaultNote>> GetNotesByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultProjectNote>()
                .AsNoTracking()
                .Include(pn => pn.Note)
                .Where(pn => pn.ProjectID == projectId)
                .Select(pn => pn.Note)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultProjectNoteQueryService), $"Retrieved {results.Count} VaultNotes for project [{projectId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string projectId, string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultProjectNote>()
                .AnyAsync(pn => pn.ProjectID == projectId && pn.NoteID == noteId, ct);

            _logger.Log(nameof(VaultProjectNoteQueryService), $"Checked existence of VaultProjectNote for project [{projectId}] note [{noteId}]: {result}");
            return result;
        }

        public async Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultProjectNote>()
                .CountAsync(pn => pn.ProjectID == projectId, ct);

            _logger.Log(nameof(VaultProjectNoteQueryService), $"Counted {result} VaultProjectNotes for project [{projectId}]");
            return result;
        }
    }
}
