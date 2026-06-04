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
    public sealed class VaultWorkflowNoteQueryService : IVaultWorkflowNoteQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowNoteQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowNote?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultWorkflowNoteQueryService), $"Retrieved VaultWorkflowNote [{id}]");
            return result;
        }

        public async Task<List<VaultWorkflowNote>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowNote>()
                .AsNoTracking()
                .Where(x => x.WorkflowID == workflowId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowNoteQueryService), $"Retrieved {results.Count} VaultWorkflowNotes for workflow [{workflowId}]");
            return results;
        }

        public async Task<List<VaultWorkflowNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowNote>()
                .AsNoTracking()
                .Where(x => x.NoteID == noteId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowNoteQueryService), $"Retrieved {results.Count} VaultWorkflowNotes for note [{noteId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowId, string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNote>()
                .AnyAsync(x => x.WorkflowID == workflowId && x.NoteID == noteId, ct);

            _logger.Log(nameof(VaultWorkflowNoteQueryService), $"VaultWorkflowNote for workflow [{workflowId}] note [{noteId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNote>()
                .CountAsync(x => x.WorkflowID == workflowId, ct);

            _logger.Log(nameof(VaultWorkflowNoteQueryService), $"Counted {result} VaultWorkflowNotes for workflow [{workflowId}]");
            return result;
        }
    }
}
