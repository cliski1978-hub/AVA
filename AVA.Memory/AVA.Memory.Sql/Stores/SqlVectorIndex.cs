using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Data.Entities;
using AVA.Memory.Sql.Context;
using AVA.Memory.Sql.Mappers;
using Microsoft.EntityFrameworkCore;

namespace AVA.Memory.Sql.Stores
{
    /// <summary>
    /// SQL-backed implementation of <see cref="IVectorIndex"/> using EF Core.
    /// Performs cosine-similarity search over persisted <see cref="MemoryVector"/> data.
    /// </summary>
    public sealed class SqlVectorIndex : IVectorIndex
    {
        private readonly IDbContextFactory<MemoryDbContext> _dbFactory;

        public SqlVectorIndex(IDbContextFactory<MemoryDbContext> dbFactory)
        {
            _dbFactory = dbFactory ?? throw new ArgumentNullException(nameof(dbFactory));
        }

        // --------------------------------------------------------------------
        // ADD / UPSERT
        // --------------------------------------------------------------------
        public async Task AddAsync(MemoryRecordDto record, CancellationToken ct)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (string.IsNullOrWhiteSpace(record.ID))
                throw new ArgumentException("Record.ID is required.", nameof(record));
            if (record.Vectors == null || record.Vectors.Count == 0)
                throw new ArgumentException("Record must contain at least one vector.", nameof(record));

            await using var ctx = _dbFactory.CreateDbContext();

            // ✅ Ensure the parent MemoryRecord exists
            var existingRecord = await ctx.MemoryRecords.FirstOrDefaultAsync(r => r.ID == record.ID, ct);
            if (existingRecord == null)
            {
                existingRecord = new MemoryRecord
                {
                    ID = record.ID,
                    Text = record.Text ?? string.Empty,
                    CreatedAt = record.CreatedAt == default ? DateTime.UtcNow : record.CreatedAt,
                    UpdatedAt = DateTime.UtcNow,
                    LastAccessedAt = DateTime.UtcNow
                };

                ctx.MemoryRecords.Add(existingRecord);
                await ctx.SaveChangesAsync(ct);
            }
            else
            {
                // ✅ Keep existing but ensure LastAccessedAt is valid
                existingRecord.UpdatedAt = DateTime.UtcNow;
                if (existingRecord.LastAccessedAt == default)
                    existingRecord.LastAccessedAt = DateTime.UtcNow;

                ctx.MemoryRecords.Update(existingRecord);
                await ctx.SaveChangesAsync(ct);
            }

            // ✅ Replace existing vectors
            var existingVectors = ctx.MemoryVectors.Where(v => v.RecordID == record.ID);
            ctx.MemoryVectors.RemoveRange(existingVectors);
            await ctx.SaveChangesAsync(ct);

            foreach (var vec in record.Vectors)
            {
                ctx.MemoryVectors.Add(new MemoryVector
                {
                    RecordID = record.ID,
                    Index = vec.Index,
                    Value = vec.Value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }

            await ctx.SaveChangesAsync(ct);
        }


        // --------------------------------------------------------------------
        // QUERY
        // --------------------------------------------------------------------
        public async Task<IReadOnlyList<QueryHit>> QueryAsync(QueryMemoryRequest request, CancellationToken ct)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (request.Embedding == null || request.Embedding.Length == 0)
                return Array.Empty<QueryHit>();

            var queryVec = request.Embedding;
            var minScore = request.MinScore <= 0 ? 0f : request.MinScore;

            await using var db = _dbFactory.CreateDbContext();

            var candidates = await db.MemoryRecords
                .Include(r => r.Vectors)
                .Include(r => r.Tags)
                .Include(r => r.Metadata)
                .AsNoTracking()
                .ToListAsync(ct);

            var results = new List<QueryHit>();

            foreach (var rec in candidates)
            {
                if (rec.Vectors == null || rec.Vectors.Count == 0)
                    continue;

                // Tag filter
                if (request.Tags?.Length > 0)
                {
                    var recTags = rec.Tags?.Select(t => t.Tag)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? new();
                    if (!request.Tags.Any(t => recTags.Contains(t)))
                        continue;
                }

                // Metadata filter
                if (request.MetadataFilters?.Count > 0)
                {
                    bool allMatch = request.MetadataFilters.All(f =>
                        rec.Metadata.Any(m =>
                            m.Key.Equals(f.Key, StringComparison.OrdinalIgnoreCase) &&
                            m.Value?.ToString() == f.Value?.ToString()));
                    if (!allMatch)
                        continue;
                }

                var recVec = rec.Vectors.OrderBy(v => v.Index).Select(v => v.Value).ToArray();
                if (recVec.Length != queryVec.Length)
                    continue;

                var score = CosineSimilarity(queryVec, recVec);
                if (score >= minScore)
                {
                    results.Add(new QueryHit
                    {
                        Record = rec.ToDto(),
                        Score = score
                    });
                }
            }

            return results
                .OrderByDescending(r => r.Score)
                .Take(request.TopK)
                .ToList();
        }

        // --------------------------------------------------------------------
        // Utility
        // --------------------------------------------------------------------
        private static float CosineSimilarity(float[] a, float[] b)
        {
            double dot = 0, normA = 0, normB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0)
                return 0f;

            return (float)(dot / (Math.Sqrt(normA) * Math.Sqrt(normB)));
        }
    }
}
