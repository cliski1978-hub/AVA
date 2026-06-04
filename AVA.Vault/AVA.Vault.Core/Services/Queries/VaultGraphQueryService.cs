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
    public sealed class VaultGraphQueryService : IVaultGraphQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultGraphQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultGraph?> GetByIdAsync(string graphId, CancellationToken ct = default)
        {
            var result = await _db.VaultGraphs
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.ID == graphId, ct);

            _logger.Log(nameof(VaultGraphQueryService), $"Retrieved VaultGraph [{graphId}]");
            return result;
        }

        public async Task<List<VaultGraph>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var results = await _db.VaultGraphs
                .AsNoTracking()
                .Where(g => g.ProjectID == projectId)
                .OrderBy(g => g.SortOrder)
                .ThenByDescending(g => g.GeneratedAt)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultGraphQueryService), $"Retrieved {results.Count} VaultGraphs for project [{projectId}]");
            return results;
        }

        public async Task<VaultGraph?> GetLatestByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var result = await _db.VaultGraphs
                .AsNoTracking()
                .Where(g => g.ProjectID == projectId)
                .OrderByDescending(g => g.GeneratedAt)
                .FirstOrDefaultAsync(ct);

            _logger.Log(nameof(VaultGraphQueryService), $"Retrieved latest VaultGraph for project [{projectId}]");
            return result;
        }

        public async Task<bool> ExistsAsync(string graphId, CancellationToken ct = default)
        {
            var result = await _db.VaultGraphs
                .AnyAsync(g => g.ID == graphId, ct);

            _logger.Log(nameof(VaultGraphQueryService), $"VaultGraph [{graphId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var result = await _db.VaultGraphs
                .CountAsync(g => g.ProjectID == projectId, ct);

            _logger.Log(nameof(VaultGraphQueryService), $"Counted {result} VaultGraphs for project [{projectId}]");
            return result;
        }
    }
}
