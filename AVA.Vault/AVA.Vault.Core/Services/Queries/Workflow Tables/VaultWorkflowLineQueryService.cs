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
    public sealed class VaultWorkflowLineQueryService : IVaultWorkflowLineQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowLine?> GetByIdAsync(string workflowLineId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLine>()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.ID == workflowLineId, ct);

            _logger.Log(nameof(VaultWorkflowLineQueryService), $"Retrieved VaultWorkflowLine [{workflowLineId}]");
            return result;
        }

        public async Task<List<VaultWorkflowLine>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLine>()
                .AsNoTracking()
                .Where(l => l.WorkflowID == workflowId)
                .OrderBy(l => l.LineOrder)
                .ThenBy(l => l.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineQueryService), $"Retrieved {results.Count} VaultWorkflowLines for workflow [{workflowId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLine>> GetBySourceWorkflowNodeIdAsync(string sourceWorkflowNodeId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLine>()
                .AsNoTracking()
                .Where(l => l.SourceWorkflowNodeID == sourceWorkflowNodeId)
                .OrderBy(l => l.LineOrder)
                .ThenBy(l => l.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineQueryService), $"Retrieved {results.Count} VaultWorkflowLines from source node [{sourceWorkflowNodeId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLine>> GetByTargetWorkflowNodeIdAsync(string targetWorkflowNodeId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLine>()
                .AsNoTracking()
                .Where(l => l.TargetWorkflowNodeID == targetWorkflowNodeId)
                .OrderBy(l => l.LineOrder)
                .ThenBy(l => l.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineQueryService), $"Retrieved {results.Count} VaultWorkflowLines targeting node [{targetWorkflowNodeId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLine>> GetOutgoingLinesAsync(string workflowNodeId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLine>()
                .AsNoTracking()
                .Where(l => l.SourceWorkflowNodeID == workflowNodeId)
                .OrderBy(l => l.LineOrder)
                .ThenBy(l => l.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineQueryService), $"Retrieved {results.Count} outgoing VaultWorkflowLines from node [{workflowNodeId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLine>> GetIncomingLinesAsync(string workflowNodeId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLine>()
                .AsNoTracking()
                .Where(l => l.TargetWorkflowNodeID == workflowNodeId)
                .OrderBy(l => l.LineOrder)
                .ThenBy(l => l.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineQueryService), $"Retrieved {results.Count} incoming VaultWorkflowLines to node [{workflowNodeId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowLineId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLine>()
                .AnyAsync(l => l.ID == workflowLineId, ct);

            _logger.Log(nameof(VaultWorkflowLineQueryService), $"VaultWorkflowLine [{workflowLineId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLine>()
                .CountAsync(l => l.WorkflowID == workflowId, ct);

            _logger.Log(nameof(VaultWorkflowLineQueryService), $"Counted {result} VaultWorkflowLines for workflow [{workflowId}]");
            return result;
        }
    }
}
