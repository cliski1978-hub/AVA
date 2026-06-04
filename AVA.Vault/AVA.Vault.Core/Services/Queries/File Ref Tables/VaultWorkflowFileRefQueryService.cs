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
    public sealed class VaultWorkflowFileRefQueryService : IVaultWorkflowFileRefQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowFileRefQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowFileRef?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowFileRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultWorkflowFileRefQueryService), $"Retrieved VaultWorkflowFileRef [{id}]");
            return result;
        }

        public async Task<List<VaultWorkflowFileRef>> GetByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowFileRef>()
                .AsNoTracking()
                .Where(x => x.WorkflowID == workflowId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowFileRefQueryService), $"Retrieved {results.Count} VaultWorkflowFileRefs for workflow [{workflowId}]");
            return results;
        }

        public async Task<List<VaultWorkflowFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowFileRef>()
                .AsNoTracking()
                .Where(x => x.FileRefID == fileRefId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowFileRefQueryService), $"Retrieved {results.Count} VaultWorkflowFileRefs for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowId, string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowFileRef>()
                .AnyAsync(x => x.WorkflowID == workflowId && x.FileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultWorkflowFileRefQueryService), $"VaultWorkflowFileRef for workflow [{workflowId}] fileRef [{fileRefId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowIdAsync(string workflowId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowFileRef>()
                .CountAsync(x => x.WorkflowID == workflowId, ct);

            _logger.Log(nameof(VaultWorkflowFileRefQueryService), $"Counted {result} VaultWorkflowFileRefs for workflow [{workflowId}]");
            return result;
        }
    }
}
