using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Core.Vector
{
    /// <summary>
    /// Lightweight, in-memory vector index for testing and temporary retrieval.
    /// Works entirely on DTO-based records and supports tag and metadata filtering.
    /// </summary>
    public class InMemoryVectorIndex : IVectorIndex
    {
        private readonly List<MemoryRecordDto> _records = new List<MemoryRecordDto>();

        public Task AddAsync(MemoryRecordDto record, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return Task.FromCanceled(ct);

            if (record == null)
                throw new ArgumentNullException(nameof(record));

            if (record.Vectors == null || record.Vectors.Count == 0)
                throw new ArgumentException("Record must have vectors before being added.");

            _records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<QueryHit>> QueryAsync(QueryMemoryRequest request, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return Task.FromCanceled<IReadOnlyList<QueryHit>>(ct);

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if ((request.Embedding == null || request.Embedding.Length == 0) &&
                string.IsNullOrEmpty(request.Text))
            {
                return Task.FromResult<IReadOnlyList<QueryHit>>(Array.Empty<QueryHit>());
            }

            var queryVec = request.Embedding;
            if (queryVec == null)
            {
                // Embedding generation from text handled upstream (MemoryBroker)
                return Task.FromResult<IReadOnlyList<QueryHit>>(Array.Empty<QueryHit>());
            }

            var results = new List<QueryHit>();

            foreach (var rec in _records)
            {
                if (ct.IsCancellationRequested)
                    break;

                if (rec.Vectors == null || rec.Vectors.Count == 0)
                    continue;

                var recVec = rec.Vectors
                    .OrderBy(v => v.Index)
                    .Select(v => (float)v.Value)
                    .ToArray();

                // Tag filtering
                if (request.Tags != null && request.Tags.Length > 0)
                {
                    var recTags = rec.Tags != null
                        ? new HashSet<string>(rec.Tags.Select(t => t.Tag), StringComparer.OrdinalIgnoreCase)
                        : new HashSet<string>();

                    if (!request.Tags.Any(t => recTags.Contains(t)))
                        continue;
                }

                // Metadata filtering
                if (request.MetadataFilters != null && request.MetadataFilters.Count > 0)
                {
                    bool allMatch = request.MetadataFilters.All(f =>
                        rec.Metadata.Any(m => m.Key == f.Key && m.Value != null &&
                                              m.Value.ToString() == f.Value?.ToString()));
                    if (!allMatch)
                        continue;
                }

                // Compute cosine similarity
                var score = CosineSimilarity(queryVec, recVec);

                if (score >= request.MinScore)
                {
                    results.Add(new QueryHit
                    {
                        Record = rec,
                        Score = score
                    });
                }
            }

            var ordered = results
                .OrderByDescending(r => r.Score)
                .Take(request.TopK)
                .ToList();

            return Task.FromResult<IReadOnlyList<QueryHit>>(ordered);
        }

        private static double CosineSimilarity(float[] a, float[] b)
        {
            if (a.Length != b.Length) return 0.0;

            double dot = 0.0;
            double normA = 0.0;
            double normB = 0.0;

            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                normA += a[i] * a[i];
                normB += b[i] * b[i];
            }

            if (normA == 0 || normB == 0) return 0.0;
            return dot / (Math.Sqrt(normA) * Math.Sqrt(normB));
        }
    }
}
