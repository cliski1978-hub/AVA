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
    public sealed class VaultFileRefQueryService : IVaultFileRefQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultFileRefQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultFileRef?> GetByIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.VaultFileRefs
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.ID == fileRefId, ct);

            _logger.Log(nameof(VaultFileRefQueryService), $"Retrieved VaultFileRef [{fileRefId}]");
            return result;
        }

        public async Task<List<VaultFileRef>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultFileRefs
                .AsNoTracking()
                .Where(f => f.VaultID == vaultId)
                .OrderBy(f => f.FileOrder)
                .ThenBy(f => f.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefQueryService), $"Retrieved {results.Count} VaultFileRefs for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultFileRef>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var results = await _db.VaultFileRefs
                .AsNoTracking()
                .Where(f => f.ProjectID == projectId)
                .OrderBy(f => f.FileOrder)
                .ThenBy(f => f.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefQueryService), $"Retrieved {results.Count} VaultFileRefs for project [{projectId}]");
            return results;
        }

        public async Task<List<VaultFileRef>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default)
        {
            var results = await _db.VaultFileRefs
                .AsNoTracking()
                .Where(f => f.SessionID == sessionId)
                .OrderBy(f => f.FileOrder)
                .ThenBy(f => f.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefQueryService), $"Retrieved {results.Count} VaultFileRefs for session [{sessionId}]");
            return results;
        }

        public async Task<List<VaultFileRef>> SearchByVaultIdAsync(string vaultId, string searchText, CancellationToken ct = default)
        {
            var results = await _db.VaultFileRefs
                .AsNoTracking()
                .Where(f => f.VaultID == vaultId &&
                    (f.Name.Contains(searchText) || f.Path.Contains(searchText)))
                .OrderBy(f => f.FileOrder)
                .ThenBy(f => f.Name)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefQueryService), $"Searched VaultFileRefs in vault [{vaultId}] with '{searchText}' — {results.Count} results");
            return results;
        }

        public async Task<bool> ExistsAsync(string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.VaultFileRefs
                .AnyAsync(f => f.ID == fileRefId, ct);

            _logger.Log(nameof(VaultFileRefQueryService), $"VaultFileRef [{fileRefId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var result = await _db.VaultFileRefs
                .CountAsync(f => f.VaultID == vaultId, ct);

            _logger.Log(nameof(VaultFileRefQueryService), $"Counted {result} VaultFileRefs for vault [{vaultId}]");
            return result;
        }

        public async Task<List<VaultFileRef>> GetByIdsAsync(List<string> fileRefIds, CancellationToken ct = default)
        {
            if (fileRefIds == null || fileRefIds.Count == 0)
                return new List<VaultFileRef>();

            var results = await _db.VaultFileRefs
                .AsNoTracking()
                .Where(f => fileRefIds.Contains(f.ID))
                .ToListAsync(ct);

            _logger.Log(nameof(VaultFileRefQueryService), $"Retrieved {results.Count} VaultFileRefs by {fileRefIds.Count} IDs");
            return results;
        }
    }
}
