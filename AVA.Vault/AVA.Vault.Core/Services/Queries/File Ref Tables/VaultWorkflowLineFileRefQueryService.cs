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
    public sealed class VaultWorkflowLineFileRefQueryService : IVaultWorkflowLineFileRefQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineFileRefQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowLineFileRef?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineFileRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultWorkflowLineFileRefQueryService), $"Retrieved VaultWorkflowLineFileRef [{id}]");
            return result;
        }

        public async Task<List<VaultWorkflowLineFileRef>> GetByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineFileRef>()
                .AsNoTracking()
                .Where(x => x.WorkflowLineID == workflowLineId)
                .OrderBy(x => x.FileOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineFileRefQueryService), $"Retrieved {results.Count} VaultWorkflowLineFileRefs for line [{workflowLineId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLineFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineFileRef>()
                .AsNoTracking()
                .Where(x => x.FileRefID == fileRefId)
                .OrderBy(x => x.FileOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineFileRefQueryService), $"Retrieved {results.Count} VaultWorkflowLineFileRefs for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowLineId, string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineFileRef>()
                .AnyAsync(x => x.WorkflowLineID == workflowLineId && x.FileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultWorkflowLineFileRefQueryService), $"VaultWorkflowLineFileRef for line [{workflowLineId}] fileRef [{fileRefId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineFileRef>()
                .CountAsync(x => x.WorkflowLineID == workflowLineId, ct);

            _logger.Log(nameof(VaultWorkflowLineFileRefQueryService), $"Counted {result} VaultWorkflowLineFileRefs for line [{workflowLineId}]");
            return result;
        }
    }
}
