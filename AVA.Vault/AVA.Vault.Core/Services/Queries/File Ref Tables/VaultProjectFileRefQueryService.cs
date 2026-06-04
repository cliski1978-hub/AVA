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
    public sealed class VaultProjectFileRefQueryService : IVaultProjectFileRefQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultProjectFileRefQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultProjectFileRef?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultProjectFileRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultProjectFileRefQueryService), $"Retrieved VaultProjectFileRef [{id}]");
            return result;
        }

        public async Task<List<VaultProjectFileRef>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultProjectFileRef>()
                .AsNoTracking()
                .Where(x => x.ProjectID == projectId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultProjectFileRefQueryService), $"Retrieved {results.Count} VaultProjectFileRefs for project [{projectId}]");
            return results;
        }

        public async Task<List<VaultProjectFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultProjectFileRef>()
                .AsNoTracking()
                .Where(x => x.FileRefID == fileRefId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultProjectFileRefQueryService), $"Retrieved {results.Count} VaultProjectFileRefs for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string projectId, string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultProjectFileRef>()
                .AnyAsync(x => x.ProjectID == projectId && x.FileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultProjectFileRefQueryService), $"VaultProjectFileRef for project [{projectId}] fileRef [{fileRefId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultProjectFileRef>()
                .CountAsync(x => x.ProjectID == projectId, ct);

            _logger.Log(nameof(VaultProjectFileRefQueryService), $"Counted {result} VaultProjectFileRefs for project [{projectId}]");
            return result;
        }
    }
}
