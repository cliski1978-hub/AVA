using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Data.Entities;
using AVA.Memory.Sql.Context;
using AVA.Memory.Sql.Mappers;
using Microsoft.EntityFrameworkCore;

namespace AVA.Memory.Sql.Stores
{
    /// <summary>
    /// SQL-backed implementation of IAssociationStore using EF Core and DTO mapping.
    /// Persists and retrieves AssociationEdges between MemoryRecords.
    /// Now uses IDbContextFactory for safe lifetime handling.
    /// </summary>
    public class SqlAssociationStore : IAssociationStore
    {
        private readonly IDbContextFactory<MemoryDbContext> _dbFactory;

        public SqlAssociationStore(IDbContextFactory<MemoryDbContext> dbFactory)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        /// <inheritdoc />
        public async Task<string> UpsertAsync(AssociationEdgeDto edgeDto, CancellationToken ct)
        {
            if (edgeDto == null)
                throw new ArgumentNullException(nameof(edgeDto));

            await using var db = _dbFactory.CreateDbContext();

            var existing = await db.AssociationEdges
                .FirstOrDefaultAsync(e =>
                    e.FromRecordID == edgeDto.FromID &&
                    e.ToRecordID == edgeDto.ToID &&
                    e.Type == edgeDto.Type,
                    ct);

            if (existing == null)
            {
                var entity = edgeDto.ToEntity();
                await db.AssociationEdges.AddAsync(entity, ct);
                await db.SaveChangesAsync(ct);
                return entity.ID;
            }
            else
            {
                existing.Weight = edgeDto.Weight;
                existing.Type = edgeDto.Type;
                existing.UpdatedAt = DateTime.UtcNow;
                db.AssociationEdges.Update(existing);
                await db.SaveChangesAsync(ct);
                return existing.ID;
            }
        }

        /// <inheritdoc />
        public async Task<AssociationEdgeDto?> GetAsync(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            await using var db = _dbFactory.CreateDbContext();

            var entity = await db.AssociationEdges
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.ID == id, ct);

            return entity?.ToDto();
        }

        /// <inheritdoc />
        public async Task<bool> DeleteAsync(string id, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(id))
                return false;

            await using var db = _dbFactory.CreateDbContext();

            var entity = await db.AssociationEdges
                .FirstOrDefaultAsync(e => e.ID == id, ct);

            if (entity == null)
                return false;

            db.AssociationEdges.Remove(entity);
            await db.SaveChangesAsync(ct);
            return true;
        }

        /// <inheritdoc />
        public async Task<(IReadOnlyList<AssociationEdgeDto> Items, int Total)> ListAsync(int skip, int take, CancellationToken ct)
        {
            await using var db = _dbFactory.CreateDbContext();

            var total = await db.AssociationEdges.CountAsync(ct);
            var entities = await db.AssociationEdges
                .AsNoTracking()
                .OrderByDescending(e => e.UpdatedAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync(ct);

            var items = entities.Select(e => e.ToDto()).ToList();
            return (items, total);
        }
    }
}
