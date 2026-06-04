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
    public sealed class VaultWorkflowNodeNoteQueryService : IVaultWorkflowNodeNoteQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowNodeNoteQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowNodeNote?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNodeNote>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultWorkflowNodeNoteQueryService), $"Retrieved VaultWorkflowNodeNote [{id}]");
            return result;
        }

        public async Task<List<VaultWorkflowNodeNote>> GetByWorkflowNodeIdAsync(string workflowNodeId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowNodeNote>()
                .AsNoTracking()
                .Where(x => x.WorkflowNodeID == workflowNodeId)
                .OrderBy(x => x.NoteOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowNodeNoteQueryService), $"Retrieved {results.Count} VaultWorkflowNodeNotes for node [{workflowNodeId}]");
            return results;
        }

        public async Task<List<VaultWorkflowNodeNote>> GetByNoteIdAsync(string noteId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowNodeNote>()
                .AsNoTracking()
                .Where(x => x.NoteID == noteId)
                .OrderBy(x => x.NoteOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowNodeNoteQueryService), $"Retrieved {results.Count} VaultWorkflowNodeNotes for note [{noteId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowNodeId, string noteId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNodeNote>()
                .AnyAsync(x => x.WorkflowNodeID == workflowNodeId && x.NoteID == noteId, ct);

            _logger.Log(nameof(VaultWorkflowNodeNoteQueryService), $"VaultWorkflowNodeNote for node [{workflowNodeId}] note [{noteId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowNodeIdAsync(string workflowNodeId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNodeNote>()
                .CountAsync(x => x.WorkflowNodeID == workflowNodeId, ct);

            _logger.Log(nameof(VaultWorkflowNodeNoteQueryService), $"Counted {result} VaultWorkflowNodeNotes for node [{workflowNodeId}]");
            return result;
        }
    }
}
