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
    public sealed class VaultSessionQueryService : IVaultSessionQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultSessionQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultSession?> GetByIdAsync(string sessionId, CancellationToken ct = default)
        {
            var result = await _db.VaultSessions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ID == sessionId, ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Retrieved VaultSession [{sessionId}]");
            return result;
        }

        public async Task<List<VaultSession>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultSessions
                .AsNoTracking()
                .Where(s => s.VaultID == vaultId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Retrieved {results.Count} VaultSessions for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultSession>> GetByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var results = await _db.VaultSessions
                .AsNoTracking()
                .Where(s => s.ProjectID == projectId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Retrieved {results.Count} VaultSessions for project [{projectId}]");
            return results;
        }

        public async Task<List<VaultSession>> GetActiveByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultSessions
                .AsNoTracking()
                .Where(s => s.VaultID == vaultId && s.IsActive)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Retrieved {results.Count} active VaultSessions for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultSession>> GetPinnedByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultSessions
                .AsNoTracking()
                .Where(s => s.VaultID == vaultId && s.IsPinned)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Retrieved {results.Count} pinned VaultSessions for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultSession>> GetTemplatesByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultSessions
                .AsNoTracking()
                .Where(s => s.VaultID == vaultId && s.IsTemplate)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Retrieved {results.Count} template VaultSessions for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultSession>> GetRecentByVaultIdAsync(string vaultId, int take, CancellationToken ct = default)
        {
            var results = await _db.VaultSessions
                .AsNoTracking()
                .Where(s => s.VaultID == vaultId)
                .OrderByDescending(s => s.LastActiveAt ?? s.UpdatedAt)
                .Take(take)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Retrieved {results.Count} recent VaultSessions for vault [{vaultId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string sessionId, CancellationToken ct = default)
        {
            var result = await _db.VaultSessions
                .AnyAsync(s => s.ID == sessionId, ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Checked existence of VaultSession [{sessionId}]: {result}");
            return result;
        }

        public async Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var result = await _db.VaultSessions
                .CountAsync(s => s.VaultID == vaultId, ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Counted {result} VaultSessions for vault [{vaultId}]");
            return result;
        }

        public async Task<int> CountByProjectIdAsync(string projectId, CancellationToken ct = default)
        {
            var result = await _db.VaultSessions
                .CountAsync(s => s.ProjectID == projectId, ct);

            _logger.Log(nameof(VaultSessionQueryService), $"Counted {result} VaultSessions for project [{projectId}]");
            return result;
        }
    }
}
