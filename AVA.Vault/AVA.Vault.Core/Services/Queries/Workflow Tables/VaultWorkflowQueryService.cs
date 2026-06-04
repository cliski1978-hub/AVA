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
    public sealed class VaultWorkflowQueryService : IVaultWorkflowQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflow> GetByIdAsync(string workflowId, CancellationToken ct = default)
        {
            _logger.Log(nameof(VaultWorkflowQueryService), $"Retrieved workflow [{workflowId}]");
            return await _db.Set<VaultWorkflow>()
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.ID == workflowId, ct);
        }

        public async Task<List<VaultWorkflow>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflow>()
                .AsNoTracking()
                .Where(w => w.ProjectID == projectId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowQueryService), $"Retrieved {results.Count} workflows for project [{projectId}]");
            return results;
        }

        public async Task<List<VaultWorkflow>> GetByStatusAsync(string projectId, string status, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflow>()
                .AsNoTracking()
                .Where(w => w.ProjectID == projectId && w.Status == status)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowQueryService), $"Retrieved {results.Count} workflows for project [{projectId}] with status [{status}]");
            return results;
        }

        public async Task<List<VaultWorkflow>> GetByWorkflowTypeAsync(string projectId, string workflowType, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflow>()
                .AsNoTracking()
                .Where(w => w.ProjectID == projectId && w.WorkflowType == workflowType)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowQueryService), $"Retrieved {results.Count} workflows for project [{projectId}] of type [{workflowType}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowId, CancellationToken ct = default)
        {
            var exists = await _db.Set<VaultWorkflow>()
                .AnyAsync(w => w.ID == workflowId, ct);

            _logger.Log(nameof(VaultWorkflowQueryService), $"Workflow [{workflowId}] exists: {exists}");
            return exists;
        }

        public async Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var count = await _db.Set<VaultWorkflow>()
                .CountAsync(w => w.ProjectID == projectId, ct);

            _logger.Log(nameof(VaultWorkflowQueryService), $"Counted {count} workflows for project [{projectId}]");
            return count;
        }

        public async Task<List<VaultWorkflow>> SearchByProjectIdAsync(string projectId, string searchText, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflow>()
                .AsNoTracking()
                .Where(w => w.ProjectID == projectId &&
                    (w.Name.Contains(searchText) ||
                     (w.Description != null && w.Description.Contains(searchText))))
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowQueryService), $"Searched workflows in project [{projectId}] with '{searchText}' — {results.Count} results");
            return results;
        }
    }
}
