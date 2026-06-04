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
    public sealed class VaultWorkflowNodeQueryService : IVaultWorkflowNodeQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowNodeQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowNode?> GetByIdAsync(string workflowNodeId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNode>()
                .AsNoTracking()
                .FirstOrDefaultAsync(n => n.ID == workflowNodeId, ct);

            _logger.Log(nameof(VaultWorkflowNodeQueryService), $"Retrieved VaultWorkflowNode [{workflowNodeId}]");
            return result;
        }

        public async Task<List<VaultWorkflowNode>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowNode>()
                .AsNoTracking()
                .Where(n => n.WorkflowID == workflowId)
                .OrderBy(n => n.NodeOrder)
                .ThenBy(n => n.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowNodeQueryService), $"Retrieved {results.Count} VaultWorkflowNodes for workflow [{workflowId}]");
            return results;
        }

        public async Task<List<VaultWorkflowNode>> GetByWorkflowIdAndTypeAsync(string workflowId, string nodeType, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowNode>()
                .AsNoTracking()
                .Where(n => n.WorkflowID == workflowId && n.NodeType == nodeType)
                .OrderBy(n => n.NodeOrder)
                .ThenBy(n => n.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowNodeQueryService), $"Retrieved {results.Count} VaultWorkflowNodes for workflow [{workflowId}] of type [{nodeType}]");
            return results;
        }

        public async Task<List<VaultWorkflowNode>> GetByStatusAsync(string workflowId, string status, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowNode>()
                .AsNoTracking()
                .Where(n => n.WorkflowID == workflowId && n.Status == status)
                .OrderBy(n => n.NodeOrder)
                .ThenBy(n => n.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowNodeQueryService), $"Retrieved {results.Count} VaultWorkflowNodes for workflow [{workflowId}] with status [{status}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowNodeId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNode>()
                .AnyAsync(n => n.ID == workflowNodeId, ct);

            _logger.Log(nameof(VaultWorkflowNodeQueryService), $"VaultWorkflowNode [{workflowNodeId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNode>()
                .CountAsync(n => n.WorkflowID == workflowId, ct);

            _logger.Log(nameof(VaultWorkflowNodeQueryService), $"Counted {result} VaultWorkflowNodes for workflow [{workflowId}]");
            return result;
        }
    }
}
