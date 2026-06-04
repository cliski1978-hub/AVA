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
    public sealed class VaultWorkflowLineStepQueryService : IVaultWorkflowLineStepQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultWorkflowLineStepQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultWorkflowLineStep?> GetByIdAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineStep>()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ID == workflowLineStepId, ct);

            _logger.Log(nameof(VaultWorkflowLineStepQueryService), $"Retrieved VaultWorkflowLineStep [{workflowLineStepId}]");
            return result;
        }

        public async Task<List<VaultWorkflowLineStep>> GetByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineStep>()
                .AsNoTracking()
                .Where(s => s.WorkflowLineID == workflowLineId)
                .OrderBy(s => s.StepOrder)
                .ThenBy(s => s.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineStepQueryService), $"Retrieved {results.Count} VaultWorkflowLineSteps for line [{workflowLineId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLineStep>> GetRequiredByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineStep>()
                .AsNoTracking()
                .Where(s => s.WorkflowLineID == workflowLineId && s.IsRequired)
                .OrderBy(s => s.StepOrder)
                .ThenBy(s => s.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineStepQueryService), $"Retrieved {results.Count} required VaultWorkflowLineSteps for line [{workflowLineId}]");
            return results;
        }

        public async Task<List<VaultWorkflowLineStep>> GetByStepTypeAsync(string workflowLineId, string stepType, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultWorkflowLineStep>()
                .AsNoTracking()
                .Where(s => s.WorkflowLineID == workflowLineId && s.StepType == stepType)
                .OrderBy(s => s.StepOrder)
                .ThenBy(s => s.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultWorkflowLineStepQueryService), $"Retrieved {results.Count} VaultWorkflowLineSteps for line [{workflowLineId}] of type [{stepType}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string workflowLineStepId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineStep>()
                .AnyAsync(s => s.ID == workflowLineStepId, ct);

            _logger.Log(nameof(VaultWorkflowLineStepQueryService), $"VaultWorkflowLineStep [{workflowLineStepId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByWorkflowLineIdAsync(string workflowLineId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultWorkflowLineStep>()
                .CountAsync(s => s.WorkflowLineID == workflowLineId, ct);

            _logger.Log(nameof(VaultWorkflowLineStepQueryService), $"Counted {result} VaultWorkflowLineSteps for line [{workflowLineId}]");
            return result;
        }
    }
}
