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
    public sealed class VaultWorkflowNodeFileRefQueryService : IVaultWorkflowNodeFileRefQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowNodeFileRefQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowNodeFileRef?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNodeFileRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultWorkflowNodeFileRefQueryService), $"Retrieved VaultWorkflowNodeFileRef [{id}]");
            return result;
        }

        public async Task<List<VaultWorkflowNodeFileRef>> GetByWorkflowNodeIdAsync(string workflowNodeId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowNodeFileRef>()
                .AsNoTracking()
                .Where(x => x.WorkflowNodeID == workflowNodeId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowNodeFileRefQueryService), $"Retrieved {results.Count} VaultWorkflowNodeFileRefs for node [{workflowNodeId}]");
            return results;
        }

        public async Task<List<VaultWorkflowNodeFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowNodeFileRef>()
                .AsNoTracking()
                .Where(x => x.FileRefID == fileRefId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowNodeFileRefQueryService), $"Retrieved {results.Count} VaultWorkflowNodeFileRefs for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowNodeId, string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNodeFileRef>()
                .AnyAsync(x => x.WorkflowNodeID == workflowNodeId && x.FileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultWorkflowNodeFileRefQueryService), $"VaultWorkflowNodeFileRef for node [{workflowNodeId}] fileRef [{fileRefId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowNodeIdAsync(string workflowNodeId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowNodeFileRef>()
                .CountAsync(x => x.WorkflowNodeID == workflowNodeId, ct);

            _logger.Log(nameof(VaultWorkflowNodeFileRefQueryService), $"Counted {result} VaultWorkflowNodeFileRefs for node [{workflowNodeId}]");
            return result;
        }
    }
}
