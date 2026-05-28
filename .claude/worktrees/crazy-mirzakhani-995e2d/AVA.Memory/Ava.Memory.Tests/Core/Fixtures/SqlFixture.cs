using AVA.Memory.Sql.Context;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Fixtures
{
    /// <summary>
    /// Ensures the SQL database for AVA.Memory is available and correctly initialized.
    /// Can also truncate tables between runs to maintain clean test state.
    /// </summary>
    public sealed class SqlFixture : IDisposable
    {
        private readonly TestContextFactory _contextFactory;
        private readonly TestConfig _config;

        public MemoryDbContext CreateDbContext() => _contextFactory.CreateDbContext();

        public SqlFixture(TestConfig config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            var options = new DbContextOptionsBuilder<MemoryDbContext>()
                .UseSqlServer(_config.ConnectionString)
                .Options;

            _contextFactory = new TestContextFactory(options);

            using var ctx = _contextFactory.CreateDbContext();
            ctx.Database.EnsureCreated();

            TestContext.WriteLine($"[SqlFixture] Connected to database: {ctx.Database.GetConnectionString()}");
        }

        /// <summary>
        /// Applies migrations and verifies all core tables exist.
        /// </summary>
        public async Task VerifySchemaAsync(CancellationToken ct = default)
        {
            await using var ctx = _contextFactory.CreateDbContext();
            await ctx.Database.MigrateAsync(ct);

            var tables = new[]
            {
                nameof(ctx.MemoryRecords),
                nameof(ctx.MemoryVectors),
                nameof(ctx.MemoryTags),
                nameof(ctx.MemoryMetadata),
                nameof(ctx.AssociationEdges)
            };

            foreach (var table in tables)
            {
                var exists = await TableExistsAsync(ctx, table, ct);
                if (!exists)
                    throw new InvalidOperationException($"[SqlFixture] Expected table '{table}' not found in database.");
            }

            TestContext.WriteLine("[SqlFixture] Verified all expected tables exist.");
        }

        /// <summary>
        /// Clears all test data from core tables for deterministic test runs.
        /// </summary>
        public async Task TruncateAllAsync(CancellationToken ct = default)
        {
            await using var ctx = _contextFactory.CreateDbContext();

            // This approach uses raw SQL TRUNCATE where allowed, otherwise DELETE fallback
            var tableNames = new[]
            {
                "MemoryVectors",
                "MemoryTags",
                "MemoryMetadata",
                "MemoryRecords",
                "AssociationEdges"
            };

            foreach (var table in tableNames)
            {
                try
                {
                    await ctx.Database.ExecuteSqlRawAsync($"TRUNCATE TABLE [{table}];", ct);
                }
                catch
                {
                    await ctx.Database.ExecuteSqlRawAsync($"DELETE FROM [{table}];", ct);
                }
            }

            await ctx.SaveChangesAsync(ct);
            TestContext.WriteLine("[SqlFixture] Truncated all Memory and Association tables.");
        }

        private static async Task<bool> TableExistsAsync(MemoryDbContext ctx, string tableName, CancellationToken ct)
        {
            var sql = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = @p0";
            var count = await ctx.Database.ExecuteSqlRawAsync(sql, new object[] { tableName }, ct);
            return count > 0;
        }

        public void Dispose()
        {
            // Context factory cleanup
        }
    }
}
