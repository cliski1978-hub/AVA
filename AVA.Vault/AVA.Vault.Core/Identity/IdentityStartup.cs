using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AVA.Identity.Core.Extensions;
using AVA.Identity.Core.Services;
using AVA.Vault.Core.Data;
using AVA.Vault.Core.Identity;
using AVA.Vault.Core.Interfaces;
using AVA.Identity.Abstractions;
using AVA.Vault.Core.Data.Entities;

namespace AVA.Vault.Core.Identity
{
    /// <summary>
    /// Centralizes all identity-related initialization for the Vault module.
    /// - Registers IdentityCore logic (generators, validators, stamp builders)
    /// - Registers Vault's identity persistence (ModuleIdentity)
    /// - Seeds identity on first run
    /// - Exposes providers for UPS and internal use
    /// </summary>
    public static class IdentityStartup
    {
        /// <summary>
        /// Registers all identity-related services the Vault module needs.
        /// Call this from VaultModule AddVaultModule() BEFORE migrations.
        /// </summary>
        public static IServiceCollection AddVaultIdentity(this IServiceCollection services)
        {
            // Add IdentityCore logic (no EF, no persistence)
            services.AddIdentityCore();

            // Vault-owned identity services
            services.AddScoped<ModuleIdentitySeeder>();
            services.AddScoped<IModuleIdentityProvider, ModuleIdentityProvider>();

            return services;
        }

        /// <summary>
        /// Executes identity persistence initialization:
        /// - Runs EF migrations for VaultDbContext
        /// - Seeds ModuleIdentity entry if missing
        /// This MUST run after BuildServiceProvider().
        /// </summary>
        public static void InitializeVaultIdentity(IServiceProvider serviceProvider)
        {
            // 1. Run EF migrations for Vault (Identity is stored here)
            var db = serviceProvider.GetRequiredService<VaultDbContext>();
            db.Database.Migrate();

            // 2. Seed persistent module identity
            var seeder = serviceProvider.GetRequiredService<ModuleIdentitySeeder>();
            seeder.EnsureSeeded();
        }
    }
}
