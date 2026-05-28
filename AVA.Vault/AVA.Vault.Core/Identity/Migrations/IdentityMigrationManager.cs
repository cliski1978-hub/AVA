using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AVA.Identity.Core.Data;

namespace AVA.Vault.Core.Identity.Migrations
{
    /// <summary>
    /// Handles creation and migration of the Identity database when Vault runs
    /// in Local Identity mode. Works directly against IdentityDbContext from
    /// AVA.Identity.Core.
    ///
    /// Even if no migrations exist (as in this version of Identity.Core),
    /// MigrateAsync() is still required to ensure database + schema creation.
    /// </summary>
    public class IdentityMigrationManager
    {
        private readonly ILogger<IdentityMigrationManager> _logger;

        public IdentityMigrationManager(ILogger<IdentityMigrationManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Ensures the Identity database exists and applies any migrations
        /// defined in the consuming Vault module. Identity.Core itself does not
        /// ship with migrations, so all migrations will be owned by Vault.
        /// </summary>
        public async Task ApplyMigrationsAsync(IdentityDbContext db)
        {
            try
            {
                _logger.LogInformation("Preparing Identity database for use…");

                await EnsurePhysicalDatabaseExistsAsync(db);

                // Even if there are no migrations today, this will:
                //  - Create the database
                //  - Create all tables from IdentityDbContext model
                _logger.LogInformation("Executing EF Core Identity schema sync…");
                await db.Database.MigrateAsync();

                _logger.LogInformation("Identity database is ready.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Identity migration failed.");
                throw;
            }
        }

        /// <summary>
        /// Returns true if there are no pending migrations. In this build of the
        /// Identity module, IdentityDbContext ships with zero migrations, so this
        /// will almost always return true unless Vault defines its own.
        /// </summary>
        public async Task<bool> IsUpToDateAsync(IdentityDbContext db)
        {
            var pending = await db.Database.GetPendingMigrationsAsync();
            return !pending.Any();
        }

        /// <summary>
        /// Ensures directories exist for SQLite so EF Core can create the .db file.
        /// No action is needed for SQL Server.
        /// </summary>
        private async Task EnsurePhysicalDatabaseExistsAsync(IdentityDbContext db)
        {
            var provider = db.Database.ProviderName;

            if (provider != null && provider.Contains("Sqlite", StringComparison.OrdinalIgnoreCase))
            {
                var cs = db.Database.GetConnectionString();

                var dbFile = ExtractSqlitePath(cs);
                if (!string.IsNullOrWhiteSpace(dbFile))
                {
                    var dir = Path.GetDirectoryName(dbFile);
                    if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                        _logger.LogInformation("Created Identity SQLite directory: {Dir}", dir);
                    }
                }
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Extracts SQLite database file path from a connection string.
        /// Supports common patterns: Data Source=, Filename=
        /// </summary>
        private static string? ExtractSqlitePath(string? conn)
        {
            if (string.IsNullOrWhiteSpace(conn))
                return null;

            foreach (var key in new[] { "Data Source=", "Filename=" })
            {
                var idx = conn.IndexOf(key, StringComparison.OrdinalIgnoreCase);
                if (idx >= 0)
                {
                    var start = idx + key.Length;
                    var semi = conn.IndexOf(';', start);
                    return semi > start ? conn.Substring(start, semi - start) : conn[start..];
                }
            }

            return null;
        }
    }
}
