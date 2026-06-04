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
    public sealed class VaultSessionFileRefQueryService : IVaultSessionFileRefQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultSessionFileRefQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultSessionFileRef?> GetByIdAsync(string id, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultSessionFileRef>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ID == id, ct);

            _logger.Log(nameof(VaultSessionFileRefQueryService), $"Retrieved VaultSessionFileRef [{id}]");
            return result;
        }

        public async Task<List<VaultSessionFileRef>> GetBySessionIdAsync(string sessionId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultSessionFileRef>()
                .AsNoTracking()
                .Where(x => x.SessionID == sessionId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionFileRefQueryService), $"Retrieved {results.Count} VaultSessionFileRefs for session [{sessionId}]");
            return results;
        }

        public async Task<List<VaultSessionFileRef>> GetByFileRefIdAsync(string fileRefId, CancellationToken ct = default)
        {
            var results = await _db.Set<VaultSessionFileRef>()
                .AsNoTracking()
                .Where(x => x.FileRefID == fileRefId)
                .OrderBy(x => x.SortOrder)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionFileRefQueryService), $"Retrieved {results.Count} VaultSessionFileRefs for fileRef [{fileRefId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string sessionId, string fileRefId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultSessionFileRef>()
                .AnyAsync(x => x.SessionID == sessionId && x.FileRefID == fileRefId, ct);

            _logger.Log(nameof(VaultSessionFileRefQueryService), $"VaultSessionFileRef for session [{sessionId}] fileRef [{fileRefId}] exists: {result}");
            return result;
        }

        public async Task<int> CountBySessionIdAsync(string sessionId, CancellationToken ct = default)
        {
            var result = await _db.Set<VaultSessionFileRef>()
                .CountAsync(x => x.SessionID == sessionId, ct);

            _logger.Log(nameof(VaultSessionFileRefQueryService), $"Counted {result} VaultSessionFileRefs for session [{sessionId}]");
            return result;
        }
    }
}
