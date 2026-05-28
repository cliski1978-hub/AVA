using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

using AVA.Identity.Core.Models;
using AVA.Identity.Core.Data;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core.Identity.Bootstrap
{
    /// <summary>
    /// Bootstraps IdentityDbContext for Vault when running in Local Identity mode.
    /// Ensures that:
    ///   - Identity database is created & migrated
    ///   - Node identity exists
    ///   - Vault module identity exists (vault-specific)
    /// </summary>
    public class IdentityBootstrap
    {
        private readonly IdentityDbContext _db;
        private readonly VaultInstanceConfig _config;
        private readonly ILogger<IdentityBootstrap> _logger;

        public IdentityBootstrap(
            IdentityDbContext db,
            VaultInstanceConfig config,
            ILogger<IdentityBootstrap> logger)
        {
            _db = db;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Executes migration + seeding logic.
        /// </summary>
        public async Task SeedIfMissingAsync()
        {
            _logger.LogInformation("IdentityBootstrap starting…");

            await MigrateIdentityDatabaseAsync();
            await SeedNodeIdentityAsync();
            await GetOrCreateLocalModuleIdentityAsync();

            _logger.LogInformation("IdentityBootstrap completed successfully.");
        }

        // ---------------------------------------------------------------------
        // Migrate Identity DB
        // ---------------------------------------------------------------------
        private async Task MigrateIdentityDatabaseAsync()
        {
            try
            {
                _logger.LogInformation("Applying Identity database migrations…");
                await _db.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to migrate Identity database.");
                throw;
            }
        }

        // ---------------------------------------------------------------------
        // Seed Node Identity
        // ---------------------------------------------------------------------
        private async Task SeedNodeIdentityAsync()
        {
            const string nodeId = "node:local";

            var existing = await _db.AvaIdentities
                .FirstOrDefaultAsync(x => x.AvaId == nodeId);

            if (existing != null)
                return;

            var identity = new AvaIdentity
            {
                AvaId = nodeId,
                DisplayName = "Local Node",
                Type = "node",
                Status = IdentityStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _db.AvaIdentities.Add(identity);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Seeded node identity: {Id}", nodeId);
        }

        // ---------------------------------------------------------------------
        // Get or Create Vault Module Identity (this specific Vault instance)
        // ---------------------------------------------------------------------
        public async Task<AvaIdentity> GetOrCreateLocalModuleIdentityAsync()
        {
            string moduleId = $"module:vault:{_config.VaultID}";

            var existing = await _db.AvaIdentities
                .FirstOrDefaultAsync(x => x.AvaId == moduleId);

            if (existing != null)
                return existing;

            var identity = new AvaIdentity
            {
                AvaId = moduleId,
                DisplayName = $"{_config.DisplayName} Vault Module",
                Type = "module",
                Status = IdentityStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            _db.AvaIdentities.Add(identity);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Created Vault module identity: {Id}", moduleId);

            return identity;
        }
    }
}
