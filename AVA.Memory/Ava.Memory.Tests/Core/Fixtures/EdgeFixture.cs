using System;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Data.Entities;
using AVA.Memory.Sql.Context;
using AVA.Memory.Sql.Stores;
using Microsoft.EntityFrameworkCore;

namespace AVA.Memory.Tests.Core.Fixtures
{
    /// <summary>
    /// Provides seeded AssociationEdge data for integration tests.
    /// Creates, retrieves, and cleans up edge entities.
    /// </summary>
    public sealed class EdgeFixture : IDisposable
    {
        private readonly TestContextFactory _factory;
        private readonly SqlAssociationStore _store;
        private readonly MemoryDbContext _db;

        public EdgeFixture(TestConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var options = new DbContextOptionsBuilder<MemoryDbContext>()
                .UseSqlServer(config.ConnectionString)
                .Options;

            _factory = new TestContextFactory(options);
            _db = _factory.CreateDbContext();
            _store = new SqlAssociationStore(_factory);

            _db.Database.EnsureCreated();
        }

        public async Task<AssociationEdgeDto> CreateEdgeAsync(string fromId, string toId, string type = "linked_to")
        {
            var entity = new AssociationEdge
            {
                ID = Guid.NewGuid().ToString("N"),
                FromRecordID = fromId,
                ToRecordID = toId,
                Type = type,
                Weight = 0.75,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.AssociationEdges.Add(entity);
            await _db.SaveChangesAsync();

            return new AssociationEdgeDto
            {
                ID = entity.ID,
                FromID = entity.FromRecordID,
                ToID = entity.ToRecordID,
                Type = entity.Type,
                Weight = entity.Weight,
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            };
        }

        public async Task<AssociationEdgeDto?> GetEdgeAsync(string id)
        {
            var edge = await _store.GetAsync(id, CancellationToken.None);
            return edge;
        }

        public async Task DeleteEdgeAsync(string id)
        {
            await _store.DeleteAsync(id, CancellationToken.None);
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
