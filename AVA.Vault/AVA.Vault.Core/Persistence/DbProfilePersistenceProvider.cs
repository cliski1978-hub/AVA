using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AVA.Vault.Core.Persistence
{
    public class DbProfilePersistenceProvider : IProfilePersistenceProvider
    {
        private readonly IDbContextFactory<VaultDbContext> _dbFactory;
        private readonly ILogger<DbProfilePersistenceProvider> _logger;

        public DbProfilePersistenceProvider(IDbContextFactory<VaultDbContext> dbFactory, ILogger<DbProfilePersistenceProvider> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        public async Task<List<AvaProviderProfile>> GetAllProviderProfilesAsync(CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            return await db.AvaProviderProfiles
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .ToListAsync(ct);
        }

        public async Task<List<AvaProviderProfile>> GetActiveProviderProfilesAsync(CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            return await db.AvaProviderProfiles
                .Include(p => p.ModelDefinitions.Where(m => m.IsActive))
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .ThenBy(p => p.Name)
                .AsSplitQuery()
                .ToListAsync(ct);
        }

        public async Task<AvaProviderProfile?> GetDefaultProviderProfileAsync(CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            return await db.AvaProviderProfiles
                .Include(p => p.ModelDefinitions.Where(m => m.IsActive))
                .Where(p => p.IsDefault)
                .OrderBy(p => p.SortOrder)
                .AsSplitQuery()
                .FirstOrDefaultAsync(ct);
        }

        public async Task<AvaProviderProfile?> GetProviderProfileByIdAsync(string id, CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            return await db.AvaProviderProfiles
                .Include(p => p.ModelDefinitions)
                .AsSplitQuery()
                .FirstOrDefaultAsync(p => p.ProviderProfileId == id, ct);
        }

        public async Task<AvaProviderProfile> SaveProviderProfileAsync(AvaProviderProfile profile, CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            var existing = await db.AvaProviderProfiles
                .FirstOrDefaultAsync(p => p.ProviderProfileId == profile.ProviderProfileId, ct);

            if (existing != null)
            {
                var savedApiKeyRef = existing.ApiKeySecretRef;
                var savedSecondaryRef = existing.SecondarySecretRef;

                db.Entry(existing).CurrentValues.SetValues(profile);

                if (!string.IsNullOrWhiteSpace(profile.ApiKeySecretRef))
                    existing.ApiKeySecretRef = profile.ApiKeySecretRef;
                else
                    existing.ApiKeySecretRef = savedApiKeyRef;

                if (!string.IsNullOrWhiteSpace(profile.SecondarySecretRef))
                    existing.SecondarySecretRef = profile.SecondarySecretRef;
                else
                    existing.SecondarySecretRef = savedSecondaryRef;

                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                profile.CreatedAt = DateTime.UtcNow;
                profile.UpdatedAt = DateTime.UtcNow;
                db.AvaProviderProfiles.Add(profile);
            }

            await db.SaveChangesAsync(ct);
            return existing ?? profile;
        }

        public async Task DeleteProviderProfileAsync(string id, CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            var profile = await db.AvaProviderProfiles.FindAsync([id], ct);
            if (profile != null)
            {
                db.AvaProviderProfiles.Remove(profile);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Deleted provider profile {ProfileId}", id);
            }
        }

        public async Task<List<AvaModelDefinition>> GetModelsByProviderProfileIdAsync(string profileId, CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            return await db.AvaModelDefinitions
                .Where(m => m.ProviderProfileId == profileId)
                .OrderBy(m => m.SortOrder)
                .ThenBy(m => m.DisplayName)
                .ToListAsync(ct);
        }

        public async Task<AvaModelDefinition?> GetModelDefinitionByIdAsync(string id, CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            return await db.AvaModelDefinitions.FindAsync([id], ct);
        }

        public async Task<AvaModelDefinition> SaveModelDefinitionAsync(AvaModelDefinition model, CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            var existing = await db.AvaModelDefinitions
                .FirstOrDefaultAsync(m => m.ModelDefinitionId == model.ModelDefinitionId, ct);

            if (existing != null)
            {
                var savedOverrideRef = existing.ApiKeyOverrideRef;

                db.Entry(existing).CurrentValues.SetValues(model);

                if (!string.IsNullOrWhiteSpace(model.ApiKeyOverrideRef))
                    existing.ApiKeyOverrideRef = model.ApiKeyOverrideRef;
                else
                    existing.ApiKeyOverrideRef = savedOverrideRef;

                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                model.CreatedAt = DateTime.UtcNow;
                model.UpdatedAt = DateTime.UtcNow;
                db.AvaModelDefinitions.Add(model);
            }

            await db.SaveChangesAsync(ct);
            return existing ?? model;
        }

        public async Task DeleteModelDefinitionAsync(string id, CancellationToken ct = default)
        {
            await using var db = _dbFactory.CreateDbContext();
            var found = await db.AvaModelDefinitions.FindAsync([id], ct);
            if (found != null)
            {
                db.AvaModelDefinitions.Remove(found);
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Deleted model definition {ModelId}", id);
            }
        }
    }
}
