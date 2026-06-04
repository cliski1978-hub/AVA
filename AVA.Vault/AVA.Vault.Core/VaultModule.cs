using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AVA.Vault.Core.Config;
using AVA.Vault.Core.Data;
using AVA.Vault.Core.Adapters;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services;
using AVA.Vault.Core.Services.Interfaces;
using AVA.Vault.Core.Services.Queries;
using AVA.Vault.Core.Services.Reads;

using AVA.Identity.Core.Data;
using AVA.Identity.Core.Extensions;
using AVA.Identity.Core.Services;

using AVA.Vault.Core.Identity.Bootstrap;
using AVA.Vault.Core.Identity.Migrations;
using AVA.Vault.Core.Identity.Registration;
using AVA.Vault.Core.Identity.Resolution;
using AVA.Vault.Core.Identity.Remote;

using CliskiCore.DbAPI.Interfaces;
using AVA.Identity.Core.Registry;
using AVA.Vault.Core.Data.Entities;
using AVA.Vault.Core.Identity;
using AVA.Vault.Core.Persistence;

using Microsoft.Extensions.Http;

namespace AVA.Vault.Core
{
    /// <summary>
    /// Primary orchestrator for the Vault module.
    /// Registers:
    ///  - Vault DbContext
    ///  - Identity (Embedded / LocalDatabase / Remote)
    ///  - Identity migration + seeding
    ///  - Logging + adapters
    ///  - Vault query/read services
    /// </summary>
    public static class VaultModule
    {
        public static IServiceCollection AddVaultModule(this IServiceCollection services, VaultInstanceConfig config)
        {
            //----------------------------------------------------------------------
            // Register Vault Configuration
            //----------------------------------------------------------------------
            services.AddSingleton(config);
            services.AddSingleton<IVaultIdService, VaultIdService>();

            //----------------------------------------------------------------------
            // Vault Database
            //----------------------------------------------------------------------
            services.AddDbContext<VaultDbContext>(options =>
            {
                if (!string.IsNullOrWhiteSpace(config.VaultConnectionString))
                    options.UseSqlServer(config.VaultConnectionString);
                else
                    options.UseSqlite($"Data Source={config.StoragePath}");
            });

            // Factory registration required for singleton consumers (e.g. VaultUiSyncService).
            services.AddDbContextFactory<VaultDbContext>(options =>
            {
                if (!string.IsNullOrWhiteSpace(config.VaultConnectionString))
                    options.UseSqlServer(config.VaultConnectionString);
                else
                    options.UseSqlite($"Data Source={config.StoragePath}");
            }, ServiceLifetime.Singleton);

            services.AddScoped<IVaultDbContext, VaultDbContextAdapter>();
            services.AddScoped<IDbContext, VaultDbContextAdapter>();
            services.AddScoped<IVaultMemorySyncAdapter, VaultMemorySyncAdapter>();

            //----------------------------------------------------------------------
            // Vault Query / Read Services
            //----------------------------------------------------------------------
            RegisterVaultQueryServices(services);
            RegisterVaultReadServices(services);

            //----------------------------------------------------------------------
            // Logging
            //----------------------------------------------------------------------
            services.AddSingleton<VaultLogger>();
            services.AddSingleton<IContextLogger>(sp => sp.GetRequiredService<VaultLogger>());

            //----------------------------------------------------------------------
            // Identity Mode Routing
            //----------------------------------------------------------------------
            switch (config.IdentityMode)
            {
                case IdentityMode.Embedded:
                    RegisterEmbeddedIdentity(services, config);
                    break;

                case IdentityMode.LocalDatabase:
                    RegisterLocalDatabaseIdentity(services, config);
                    break;

                case IdentityMode.Remote:
                    RegisterRemoteIdentity(services, config);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(config.IdentityMode));
            }

            return services;
        }

        private static void RegisterVaultQueryServices(IServiceCollection services)
        {
            //----------------------------------------------------------------------
            // Core table query services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultHeaderQueryService, VaultHeaderQueryService>();
            services.AddScoped<IVaultProjectQueryService, VaultProjectQueryService>();
            services.AddScoped<IVaultProjectNoteQueryService, VaultProjectNoteQueryService>();
            services.AddScoped<IVaultSessionQueryService, VaultSessionQueryService>();
            services.AddScoped<IVaultNoteQueryService, VaultNoteQueryService>();
            services.AddScoped<IVaultWorkflowQueryService, VaultWorkflowQueryService>();

            //----------------------------------------------------------------------
            // Workflow table query services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultWorkflowNodeQueryService, VaultWorkflowNodeQueryService>();
            services.AddScoped<IVaultWorkflowLineQueryService, VaultWorkflowLineQueryService>();
            services.AddScoped<IVaultWorkflowLineStepQueryService, VaultWorkflowLineStepQueryService>();

            //----------------------------------------------------------------------
            // Note join / relation query services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultHeaderNoteQueryService, VaultHeaderNoteQueryService>();
            services.AddScoped<IVaultSessionNoteQueryService, VaultSessionNoteQueryService>();
            services.AddScoped<IVaultWorkflowNoteQueryService, VaultWorkflowNoteQueryService>();
            services.AddScoped<IVaultWorkflowNodeNoteQueryService, VaultWorkflowNodeNoteQueryService>();
            services.AddScoped<IVaultWorkflowLineNoteQueryService, VaultWorkflowLineNoteQueryService>();
            services.AddScoped<IVaultWorkflowLineStepNoteQueryService, VaultWorkflowLineStepNoteQueryService>();
            services.AddScoped<IVaultNoteRelationQueryService, VaultNoteRelationQueryService>();

            //----------------------------------------------------------------------
            // File ref / relation query services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultFileRefQueryService, VaultFileRefQueryService>();
            services.AddScoped<IVaultHeaderFileRefQueryService, VaultHeaderFileRefQueryService>();
            services.AddScoped<IVaultProjectFileRefQueryService, VaultProjectFileRefQueryService>();
            services.AddScoped<IVaultSessionFileRefQueryService, VaultSessionFileRefQueryService>();
            services.AddScoped<IVaultNoteFileRefQueryService, VaultNoteFileRefQueryService>();
            services.AddScoped<IVaultFileRefNoteQueryService, VaultFileRefNoteQueryService>();
            services.AddScoped<IVaultFileRefRelationQueryService, VaultFileRefRelationQueryService>();
            services.AddScoped<IVaultWorkflowFileRefQueryService, VaultWorkflowFileRefQueryService>();
            services.AddScoped<IVaultWorkflowNodeFileRefQueryService, VaultWorkflowNodeFileRefQueryService>();
            services.AddScoped<IVaultWorkflowLineFileRefQueryService, VaultWorkflowLineFileRefQueryService>();
            services.AddScoped<IVaultWorkflowLineStepFileRefQueryService, VaultWorkflowLineStepFileRefQueryService>();

            //----------------------------------------------------------------------
            // Tag / metadata / graph query services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultTagQueryService, VaultTagQueryService>();
            services.AddScoped<IVaultNoteVaultTagQueryService, VaultNoteVaultTagQueryService>();
            services.AddScoped<IVaultMetadataQueryService, VaultMetadataQueryService>();
            services.AddScoped<IVaultGraphQueryService, VaultGraphQueryService>();

            //----------------------------------------------------------------------
            // Concrete-only query helpers
            //----------------------------------------------------------------------
            services.AddScoped<VaultChatQueries>();
            services.AddScoped<VaultAuditQueries>();
        }

        private static void RegisterVaultReadServices(IServiceCollection services)
        {
            //----------------------------------------------------------------------
            // Navigation / attached note read services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultNavigationReadService, VaultNavigationReadService>();
            services.AddScoped<IVaultHeaderNotesReadService, VaultHeaderNotesReadService>();
            services.AddScoped<IVaultProjectNotesReadService, VaultProjectNotesReadService>();
            services.AddScoped<IVaultSessionNotesReadService, VaultSessionNotesReadService>();
            services.AddScoped<IVaultWorkflowNotesReadService, VaultWorkflowNotesReadService>();

            //----------------------------------------------------------------------
            // Workflow file ref read services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultWorkflowFileRefsReadService, VaultWorkflowFileRefsReadService>();
            services.AddScoped<IVaultWorkflowNodeFileRefsReadService, VaultWorkflowNodeFileRefsReadService>();
            services.AddScoped<IVaultWorkflowLineFileRefsReadService, VaultWorkflowLineFileRefsReadService>();
            services.AddScoped<IVaultWorkflowLineStepFileRefsReadService, VaultWorkflowLineStepFileRefsReadService>();

            //----------------------------------------------------------------------
            // Workflow details / graph read services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultWorkflowDetailsReadService, VaultWorkflowDetailsReadService>();
            services.AddScoped<IVaultWorkflowGraphReadService, VaultWorkflowGraphReadService>();
            services.AddScoped<IVaultWorkflowNodeDetailsReadService, VaultWorkflowNodeDetailsReadService>();
            services.AddScoped<IVaultWorkflowNodeNotesReadService, VaultWorkflowNodeNotesReadService>();
            services.AddScoped<IVaultWorkflowLineDetailsReadService, VaultWorkflowLineDetailsReadService>();
            services.AddScoped<IVaultWorkflowLineNotesReadService, VaultWorkflowLineNotesReadService>();
            services.AddScoped<IVaultWorkflowLineStepDetailsReadService, VaultWorkflowLineStepDetailsReadService>();
            services.AddScoped<IVaultWorkflowLineStepNotesReadService, VaultWorkflowLineStepNotesReadService>();

            //----------------------------------------------------------------------
            // Note detail / usage / context read services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultNoteDetailsReadService, VaultNoteDetailsReadService>();
            services.AddScoped<IVaultNoteUsageReadService, VaultNoteUsageReadService>();
            services.AddScoped<IVaultNoteContextReadService, VaultNoteContextReadService>();

            //----------------------------------------------------------------------
            // File detail / usage / context read services
            //----------------------------------------------------------------------
            services.AddScoped<IVaultFileUsageReadService, VaultFileUsageReadService>();
            services.AddScoped<IVaultFileDetailsReadService, VaultFileDetailsReadService>();
            services.AddScoped<IVaultContextFilesReadService, VaultContextFilesReadService>();
        }

        // ======================================================================
        //  MODE 1: EMBEDDED IDENTITY
        // ======================================================================
        private static void RegisterEmbeddedIdentity(IServiceCollection services, VaultInstanceConfig config)
        {
            //--------------------------------------------------------------
            // 1. IdentityDbContext (SQLite)
            //--------------------------------------------------------------
            services.AddDbContext<IdentityDbContext>(options => options.UseSqlite($"Data Source={config.EmbeddedIdentityFullPath}"));

            //--------------------------------------------------------------
            // 2. IdentityCore baseline (StampBuilder, Validator, Coordinator)
            //--------------------------------------------------------------
            services.AddIdentityCore();

            //--------------------------------------------------------------
            // 3. Registration components
            //--------------------------------------------------------------
            services.AddScoped<IdentityMigrationManager>();
            services.AddScoped<IdentityBootstrap>();
            services.AddScoped<ModuleIdentitySeeder>();
            services.AddScoped<LocalIdentityResolver>();

            //--------------------------------------------------------------
            // 4. Provider for injected abstraction
            //--------------------------------------------------------------
            services.AddScoped<IIdentityResolver>(sp => sp.GetRequiredService<LocalIdentityResolver>());

            //--------------------------------------------------------------
            // 5. Migration + Seed
            //--------------------------------------------------------------
            if (config.AutoMigrate)
            {
                using var sp = services.BuildServiceProvider();
                var logger = sp.GetRequiredService<ILogger>();

                logger.LogInformation("Vault: Running Embedded Identity initialization.");

                var db = sp.GetRequiredService<IdentityDbContext>();
                var migrator = sp.GetRequiredService<IdentityMigrationManager>();
                var bootstrap = sp.GetRequiredService<IdentityBootstrap>();

                migrator.ApplyMigrationsAsync(db).GetAwaiter().GetResult();
                bootstrap.SeedIfMissingAsync().GetAwaiter().GetResult();

                logger.LogInformation("Vault: Embedded Identity initialization complete.");
            }
        }

        // ======================================================================
        //  MODE 2: LOCAL DATABASE IDENTITY
        // ======================================================================
        private static void RegisterLocalDatabaseIdentity(IServiceCollection services, VaultInstanceConfig config)
        {
            if (string.IsNullOrWhiteSpace(config.IdentityConnectionString))
                throw new InvalidOperationException("IdentityMode.LocalDatabase selected but IdentityConnectionString is null.");

            //--------------------------------------------------------------
            // 1. IdentityDbContext (External DB)
            //--------------------------------------------------------------
            services.AddDbContext<IdentityDbContext>(options => options.UseSqlite(config.IdentityConnectionString));
            // TODO: Switch to SQL Server automatically based on conn string detection.

            //--------------------------------------------------------------
            // 2. Identity core components
            //--------------------------------------------------------------
            services.AddIdentityCore();

            //--------------------------------------------------------------
            // 3. Registration
            //--------------------------------------------------------------
            services.AddScoped<IdentityMigrationManager>();
            services.AddScoped<IdentityBootstrap>();
            services.AddScoped<ModuleIdentitySeeder>();
            services.AddScoped<LocalIdentityResolver>();
            services.AddScoped<IIdentityResolver>(sp => sp.GetRequiredService<LocalIdentityResolver>());

            //--------------------------------------------------------------
            // 4. Migrate + Seed
            //--------------------------------------------------------------
            if (config.AutoMigrate)
            {
                using var sp = services.BuildServiceProvider();
                var logger = sp.GetRequiredService<ILogger>();

                logger.LogInformation("Vault: Running LocalDatabase Identity initialization.");

                var db = sp.GetRequiredService<IdentityDbContext>();
                var migrator = sp.GetRequiredService<IdentityMigrationManager>();
                var bootstrap = sp.GetRequiredService<IdentityBootstrap>();

                migrator.ApplyMigrationsAsync(db).GetAwaiter().GetResult();
                bootstrap.SeedIfMissingAsync().GetAwaiter().GetResult();

                logger.LogInformation("Vault: LocalDatabase Identity initialization complete.");
            }
        }

        // ======================================================================
        //  MODE 3: REMOTE IDENTITY
        // ======================================================================
        private static void RegisterRemoteIdentity(IServiceCollection services, VaultInstanceConfig config)
        {
            //--------------------------------------------------------------
            // No IdentityDbContext is registered in remote mode.
            //--------------------------------------------------------------

            //--------------------------------------------------------------
            // Remote client + Remote resolver
            //--------------------------------------------------------------
            services.AddHttpClient<RemoteIdentityClient>();
            services.AddSingleton<RemoteIdentityResolver>();
            services.AddSingleton<IIdentityResolver>(sp => sp.GetRequiredService<RemoteIdentityResolver>());

            //--------------------------------------------------------------
            // IdentityCore baseline still added for stamp builder + validator
            //--------------------------------------------------------------
            services.AddIdentityCore();
        }
    }
}