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
    public sealed class VaultHeaderQueryService : IVaultHeaderQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultHeaderQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultHeader?> GetByIdAsync(string vaultId, CancellationToken ct = default)
        {
            var result = await _db.VaultHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.ID == vaultId, ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Retrieved vault header [{vaultId}]");
            return result;
        }

        public async Task<List<VaultHeader>> GetAllAsync(CancellationToken ct = default)
        {
            var results = await _db.VaultHeaders
                .AsNoTracking()
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Retrieved {results.Count} vault headers");
            return results;
        }

        public async Task<List<VaultHeader>> GetActiveAsync(CancellationToken ct = default)
        {
            var results = await _db.VaultHeaders
                .AsNoTracking()
                .Where(h => h.IsActive)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Retrieved {results.Count} active vault headers");
            return results;
        }

        public async Task<List<VaultHeader>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default)
        {
            var results = await _db.VaultHeaders
                .AsNoTracking()
                .Where(h => h.OwnerId == ownerId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Retrieved {results.Count} vault headers for owner [{ownerId}]");
            return results;
        }

        public async Task<List<VaultHeader>> GetActiveByOwnerIdAsync(string ownerId, CancellationToken ct = default)
        {
            var results = await _db.VaultHeaders
                .AsNoTracking()
                .Where(h => h.IsActive && h.OwnerId == ownerId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Retrieved {results.Count} active vault headers for owner [{ownerId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string vaultId, CancellationToken ct = default)
        {
            var result = await _db.VaultHeaders
                .AnyAsync(h => h.ID == vaultId, ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Checked existence of vault header [{vaultId}]: {result}");
            return result;
        }

        public async Task<int> CountAsync(CancellationToken ct = default)
        {
            var result = await _db.VaultHeaders
                .CountAsync(ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Counted {result} vault headers");
            return result;
        }

        public async Task<int> CountActiveAsync(CancellationToken ct = default)
        {
            var result = await _db.VaultHeaders
                .CountAsync(h => h.IsActive, ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Counted {result} active vault headers");
            return result;
        }

        public async Task<int> CountByOwnerIdAsync(string ownerId, CancellationToken ct = default)
        {
            var result = await _db.VaultHeaders
                .CountAsync(h => h.OwnerId == ownerId, ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Counted {result} vault headers for owner [{ownerId}]");
            return result;
        }

        public async Task<List<VaultHeader>> SearchAsync(string searchText, CancellationToken ct = default)
        {
            var results = await _db.VaultHeaders
                .AsNoTracking()
                .Where(h => h.DisplayName.Contains(searchText) || (h.Description != null && h.Description.Contains(searchText)))
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Searched vault headers with text [{searchText}], found {results.Count} results");
            return results;
        }

        public async Task<List<VaultHeader>> SearchActiveAsync(string searchText, CancellationToken ct = default)
        {
            var results = await _db.VaultHeaders
                .AsNoTracking()
                .Where(h => h.IsActive && (h.DisplayName.Contains(searchText) || (h.Description != null && h.Description.Contains(searchText))))
                .ToListAsync(ct);

            _logger.Log(nameof(VaultHeaderQueryService), $"Searched active vault headers with text [{searchText}], found {results.Count} results");
            return results;
        }
    }
}
