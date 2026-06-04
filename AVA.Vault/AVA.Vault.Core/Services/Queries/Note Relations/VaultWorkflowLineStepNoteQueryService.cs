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
    public sealed class VaultWorkflowLineStepNoteQueryService : IVaultWorkflowLineStepNoteQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineStepNoteQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowLineStepNote?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineStepNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultWorkflowLineStepNoteQueryService), $"Retrieved VaultWorkflowLineStepNote [{id}]");
            return result;
        }

        public async Task<List<VaultWorkflowLineStepNote>> GetByWorkflowLineStepIdAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineStepNote>()
                .AsNoTracking()
                .Where(x => x.WorkflowLineStepID == workflowLineStepId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineStepNoteQueryService), $"Retrieved {results.Count} VaultWorkflowLineStepNotes for step [{workflowLineStepId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLineStepNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineStepNote>()
                .AsNoTracking()
                .Where(x => x.NoteID == noteId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineStepNoteQueryService), $"Retrieved {results.Count} VaultWorkflowLineStepNotes for note [{noteId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowLineStepId, string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineStepNote>()
                .AnyAsync(x => x.WorkflowLineStepID == workflowLineStepId && x.NoteID == noteId, ct);

            _logger.Log(nameof(VaultWorkflowLineStepNoteQueryService), $"VaultWorkflowLineStepNote for step [{workflowLineStepId}] note [{noteId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowLineStepIdAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineStepNote>()
                .CountAsync(x => x.WorkflowLineStepID == workflowLineStepId, ct);

            _logger.Log(nameof(VaultWorkflowLineStepNoteQueryService), $"Counted {result} VaultWorkflowLineStepNotes for step [{workflowLineStepId}]");
            return result;
        }
    }
}
