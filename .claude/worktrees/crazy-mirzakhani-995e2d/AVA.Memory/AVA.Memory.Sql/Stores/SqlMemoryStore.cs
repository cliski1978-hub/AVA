using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Data.Entities;
using AVA.Memory.Sql.Context;
using AVA.Memory.Sql.Mappers;
using Microsoft.EntityFrameworkCore;

namespace AVA.Memory.Sql.Stores
{
    /// <summary>
    /// SQL-backed implementation of IMemoryStore using EF Core and IDbContextFactory.
    /// Provides CRUD operations for persisted memory records with vector, tag, and metadata mapping.
    /// </summary>
    public sealed class SqlMemoryStore : IMemoryStore
    {
        private readonly IDbContextFactory<MemoryDbContext> _dbFactory;

        public SqlMemoryStore(IDbContextFactory<MemoryDbContext> dbFactory)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        /// <inheritdoc />
        public async Task<string> UpsertAsync(MemoryRecordDto record, CancellationToken ct)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            await using var db = _dbFactory.CreateDbContext();

            var existing = await db.MemoryRecords
                .Include(r => r.Vectors)
                .Include(r => r.Tags)
                .Include(r => r.Metadata)
                .FirstOrDefaultAsync(r => r.ID == record.ID, ct);

            if (existing == null)
            {
                var entity = record.ToEntity();
                await db.MemoryRecords.AddAsync(entity, ct);
                await db.SaveChangesAsync(ct);
                return entity.ID;
            }
            else
            {
                // Update top-level fields
                db.Entry(existing).CurrentValues.SetValues(record.ToEntity());

                // Clear and replace related collections
                db.MemoryVectors.RemoveRange(existing.Vectors);
                db.MemoryTags.RemoveRange(existing.Tags);
                db.MemoryMetadata.RemoveRange(existing.Metadata);

                existing.Vectors = record.Vectors.Select(v => v.ToEntity()).ToList();
                existing.Tags = record.Tags.Select(t => t.ToEntity()).ToList();
                existing.Metadata = record.Metadata.Select(m => m.ToEntity()).ToList();

                await db.SaveChangesAsync(ct);
                return existing.ID;
            }
        }

        /// <inheritdoc />
        public async Task<MemoryRecordDto?> GetAsync(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            await using var db = _dbFactory.CreateDbContext();

            var entity = await db.MemoryRecords
                .Include(r => r.Vectors)
                .Include(r => r.Tags)
                .Include(r => r.Metadata)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.ID == id, ct);

            return entity?.ToDto();
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            await using var db = _dbFactory.CreateDbContext();

            var entity = await db.MemoryRecords.FirstOrDefaultAsync(r => r.ID == id, ct);
            if (entity == null) return false;

            db.MemoryRecords.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<MemoryRecordDto> Items, int Total)> ListAsync(int skip, int take, CancellationToken ct)
        {
            await using var db = _dbFactory.CreateDbContext();

            var total = await db.MemoryRecords.CountAsync(ct);
            var entities = await db.MemoryRecords
                .Include(r => r.Vectors)
                .Include(r => r.Tags)
                .Include(r => r.Metadata)
                .AsNoTracking()
                .OrderByDescending(r => r.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            var items = entities.Select(e => e.ToDto()).ToList();
            return (items, total);
        }
    }
}
