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
    public sealed class VaultWorkflowLineNoteQueryService : IVaultWorkflowLineNoteQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineNoteQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowLineNote?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultWorkflowLineNoteQueryService), $"Retrieved VaultWorkflowLineNote [{id}]");
            return result;
        }

        public async Task<List<VaultWorkflowLineNote>> GetByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineNote>()
                .AsNoTracking()
                .Where(x => x.WorkflowLineID == workflowLineId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineNoteQueryService), $"Retrieved {results.Count} VaultWorkflowLineNotes for line [{workflowLineId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLineNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineNote>()
                .AsNoTracking()
                .Where(x => x.NoteID == noteId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineNoteQueryService), $"Retrieved {results.Count} VaultWorkflowLineNotes for note [{noteId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowLineId, string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineNote>()
                .AnyAsync(x => x.WorkflowLineID == workflowLineId && x.NoteID == noteId, ct);

            _logger.Log(nameof(VaultWorkflowLineNoteQueryService), $"VaultWorkflowLineNote for line [{workflowLineId}] note [{noteId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineNote>()
                .CountAsync(x => x.WorkflowLineID == workflowLineId, ct);

            _logger.Log(nameof(VaultWorkflowLineNoteQueryService), $"Counted {result} VaultWorkflowLineNotes for line [{workflowLineId}]");
            return result;
        }
    }
}
