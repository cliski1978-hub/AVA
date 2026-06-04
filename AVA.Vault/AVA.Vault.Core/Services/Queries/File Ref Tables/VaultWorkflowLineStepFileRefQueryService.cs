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
    public sealed class VaultWorkflowLineStepFileRefQueryService : IVaultWorkflowLineStepFileRefQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineStepFileRefQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowLineStepFileRef?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineStepFileRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultWorkflowLineStepFileRefQueryService), $"Retrieved VaultWorkflowLineStepFileRef [{id}]");
            return result;
        }

        public async Task<List<VaultWorkflowLineStepFileRef>> GetByWorkflowLineStepIdAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineStepFileRef>()
                .AsNoTracking()
                .Where(x => x.WorkflowLineStepID == workflowLineStepId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineStepFileRefQueryService), $"Retrieved {results.Count} VaultWorkflowLineStepFileRefs for step [{workflowLineStepId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLineStepFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineStepFileRef>()
                .AsNoTracking()
                .Where(x => x.FileRefID == fileRefId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineStepFileRefQueryService), $"Retrieved {results.Count} VaultWorkflowLineStepFileRefs for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowLineStepId, string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineStepFileRef>()
                .AnyAsync(x => x.WorkflowLineStepID == workflowLineStepId && x.FileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultWorkflowLineStepFileRefQueryService), $"VaultWorkflowLineStepFileRef for step [{workflowLineStepId}] fileRef [{fileRefId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowLineStepIdAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineStepFileRef>()
                .CountAsync(x => x.WorkflowLineStepID == workflowLineStepId, ct);

            _logger.Log(nameof(VaultWorkflowLineStepFileRefQueryService), $"Counted {result} VaultWorkflowLineStepFileRefs for step [{workflowLineStepId}]");
            return result;
        }
    }
}
