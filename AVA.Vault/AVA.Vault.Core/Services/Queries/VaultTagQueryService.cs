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
    public sealed class VaultTagQueryService : IVaultTagQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultTagQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultTag?> GetByIdAsync(string tagId, CancellationToken ct = default)
        {
            var result = await _db.VaultTags
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.ID == tagId, ct);

            _logger.Log(nameof(VaultTagQueryService), $"Retrieved VaultTag [{tagId}]");
            return result;
        }

        public async Task<List<VaultTag>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var results = await _db.VaultTags
                .AsNoTracking()
                .Where(t => t.ProjectID == projectId)
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultTagQueryService), $"Retrieved {results.Count} VaultTags for project [{projectId}]");
            return results;
        }

        public async Task<VaultTag?> GetByNameAsync(string projectId, string name, CancellationToken ct = default)
        {
            var result = await _db.VaultTags
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.ProjectID == projectId && t.Name == name, ct);

            _logger.Log(nameof(VaultTagQueryService), $"Retrieved VaultTag by name [{name}] for project [{projectId}]");
            return result;
        }

        public async Task<List<VaultTag>> SearchByProjectIdAsync(string projectId, string searchText, CancellationToken ct = default)
        {
            var results = await _db.VaultTags
                .AsNoTracking()
                .Where(t => t.ProjectID == projectId && t.Name.Contains(searchText))
                .OrderBy(t => t.SortOrder)
                .ThenBy(t => t.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultTagQueryService), $"Searched VaultTags in project [{projectId}] with '{searchText}' — {results.Count} results");
            return results;
        }

        public async Task<bool> ExistsAsync(string tagId, CancellationToken ct = default)
        {
            var result = await _db.VaultTags
                .AnyAsync(t => t.ID == tagId, ct);

            _logger.Log(nameof(VaultTagQueryService), $"VaultTag [{tagId}] exists: {result}");
            return result;
        }

        public async Task<bool> ExistsByNameAsync(string projectId, string name, CancellationToken ct = default)
        {
            var result = await _db.VaultTags
                .AnyAsync(t => t.ProjectID == projectId && t.Name == name, ct);

            _logger.Log(nameof(VaultTagQueryService), $"VaultTag by name [{name}] in project [{projectId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var result = await _db.VaultTags
                .CountAsync(t => t.ProjectID == projectId, ct);

            _logger.Log(nameof(VaultTagQueryService), $"Counted {result} VaultTags for project [{projectId}]");
            return result;
        }

        public async Task<List<VaultTag>> GetByIdsAsync(List<string> tagIds, CancellationToken ct = default)
        {
            if (tagIds == null || tagIds.Count == 0)
                return new List<VaultTag>();

            var results = await _db.VaultTags
                .AsNoTracking()
                .Where(t => tagIds.Contains(t.ID))
                .ToListAsync(ct);

            _logger.Log(nameof(VaultTagQueryService), $"Retrieved {results.Count} VaultTags by {tagIds.Count} IDs");
            return results;
        }
    }
}
