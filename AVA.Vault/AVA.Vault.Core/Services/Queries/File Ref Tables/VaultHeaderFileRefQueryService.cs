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
    public sealed class VaultHeaderFileRefQueryService : IVaultHeaderFileRefQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultHeaderFileRefQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultHeaderFileRef?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultHeaderFileRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultHeaderFileRefQueryService), $"Retrieved VaultHeaderFileRef [{id}]");
            return result;
        }

        public async Task<List<VaultHeaderFileRef>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultHeaderFileRef>()
                .AsNoTracking()
                .Where(x => x.VaultID == vaultId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderFileRefQueryService), $"Retrieved {results.Count} VaultHeaderFileRefs for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultHeaderFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultHeaderFileRef>()
                .AsNoTracking()
                .Where(x => x.FileRefID == fileRefId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderFileRefQueryService), $"Retrieved {results.Count} VaultHeaderFileRefs for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string vaultId, string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultHeaderFileRef>()
                .AnyAsync(x => x.VaultID == vaultId && x.FileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultHeaderFileRefQueryService), $"VaultHeaderFileRef for vault [{vaultId}] fileRef [{fileRefId}] exists: {result}");
            return result;
        }

        public async Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultHeaderFileRef>()
                .CountAsync(x => x.VaultID == vaultId, ct);

            _logger.Log(nameof(VaultHeaderFileRefQueryService), $"Counted {result} VaultHeaderFileRefs for vault [{vaultId}]");
            return result;
        }
    }
}
