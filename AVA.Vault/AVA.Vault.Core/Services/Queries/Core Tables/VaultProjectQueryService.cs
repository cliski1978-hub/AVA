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
    public sealed class VaultProjectQueryService : IVaultProjectQueryService
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;

        public VaultProjectQueryService(IVaultDbContext db, VaultLogger logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultProject?> GetByIdAsync(string projectId, CancellationToken ct = default)
        {
            var result = await _db.VaultProjects
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ID == projectId, ct);

            _logger.Log(nameof(VaultProjectQueryService), $"Retrieved vault project [{projectId}]");
            return result;
        }

        public async Task<List<VaultProject>> GetByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultProjects
                .AsNoTracking()
                .Where(p => p.VaultID == vaultId)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultProjectQueryService), $"Retrieved {results.Count} vault projects for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultProject>> GetActiveByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultProjects
                .AsNoTracking()
                .Where(p => p.VaultID == vaultId && !p.IsArchived)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultProjectQueryService), $"Retrieved {results.Count} active vault projects for vault [{vaultId}]");
            return results;
        }

        public async Task<List<VaultProject>> GetArchivedByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var results = await _db.VaultProjects
                .AsNoTracking()
                .Where(p => p.VaultID == vaultId && p.IsArchived)
                .ToListAsync(ct);

            _logger.Log(nameof(VaultProjectQueryService), $"Retrieved {results.Count} archived vault projects for vault [{vaultId}]");
            return results;
        }

        public async Task<bool> ExistsAsync(string projectId, CancellationToken ct = default)
        {
            var result = await _db.VaultProjects
                .AnyAsync(p => p.ID == projectId, ct);

            _logger.Log(nameof(VaultProjectQueryService), $"Checked existence of vault project [{projectId}]: {result}");
            return result;
        }

        public async Task<int> CountByVaultIdAsync(string vaultId, CancellationToken ct = default)
        {
            var result = await _db.VaultProjects
                .CountAsync(p => p.VaultID == vaultId, ct);

            _logger.Log(nameof(VaultProjectQueryService), $"Counted {result} vault projects for vault [{vaultId}]");
            return result;
        }

        public async Task<List<VaultProject>> SearchByVaultIdAsync(string vaultId, string searchText, CancellationToken ct = default)
        {
            var results = await _db.VaultProjects
                .AsNoTracking()
                .Where(p => p.VaultID == vaultId && (p.Name.Contains(searchText) || (p.Description != null && p.Description.Contains(searchText))))
                .ToListAsync(ct);

            _logger.Log(nameof(VaultProjectQueryService), $"Searched vault projects for vault [{vaultId}] with text [{searchText}], found {results.Count} results");
            return results;
        }
    }
}
