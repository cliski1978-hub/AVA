using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;

namespace AVA.Memory.Core.Vector
{
    /// <summary>
    /// Simple in-memory implementation of IVectorDBDriver.
    /// Used for testing and mock mode when no external vector database
    /// (like Qdrant or Milvus) is available.
    /// </summary>
    public class InMemoryVectorDBDriver : IVectorDBDriver
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, VectorDBRecord>> _collections
            = new(StringComparer.OrdinalIgnoreCase);

        #region Collection Management

        public Task<bool> EnsureCollectionAsync(VectorDBCollectionDto collection, CancellationToken ct = default)
        {
            _collections.TryAdd(collection.Name, new ConcurrentDictionary<string, VectorDBRecord>());
            return Task.FromResult(true);
        }

        public Task<IReadOnlyList<VectorDBCollectionDto>> ListCollectionsAsync(CancellationToken ct = default)
        {
            var list = _collections.Keys.Select(name =>
            {
                var count = _collections.TryGetValue(name, out var coll) ? coll.Count : 0;

                return new VectorDBCollectionDto
                {
                    Name = name,
                    Description = $"In-memory collection '{name}'",
                    VectorCount = count,
                    IsInitialized = true,
                    Dimension = count > 0 ? coll.Values.First().Vector.Length : 0,
                    Metric = "cosine",
                    DistanceMetric = "Cosine",
                    LastUpdated = DateTime.UtcNow
                };
            }).ToList();

            return Task.FromResult<IReadOnlyList<VectorDBCollectionDto>>(list);
        }

        public Task<bool> DeleteCollectionAsync(string name, CancellationToken ct = default)
        {
            var removed = _collections.TryRemove(name, out _);
            return Task.FromResult(removed);
        }

        #endregion

        #region CRUD Operations

        public Task UpsertAsync(VectorDBRecord record, CancellationToken ct = default)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            if (string.IsNullOrWhiteSpace(record.Collection))
                throw new ArgumentException("Record must have a valid collection name.", nameof(record));

            var collection = _collections.GetOrAdd(record.Collection, _ => new ConcurrentDictionary<string, VectorDBRecord>());
            collection[record.Id] = record;

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string id, string? collection = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentNullException(nameof(id));

            if (!string.IsNullOrWhiteSpace(collection))
            {
                if (_collections.TryGetValue(collection, out var coll))
                    coll.TryRemove(id, out _);
            }
            else
            {
                foreach (var coll in _collections.Values)
                    coll.TryRemove(id, out _);
            }

            return Task.CompletedTask;
        }

        #endregion

        #region Search

        public Task<IReadOnlyList<VectorDbSearchResult>> SearchAsync(
            float[] vector,
            int topK,
            string? filter = null,
            CancellationToken ct = default)
        {
            var results = new List<VectorDbSearchResult>();

            foreach (var collection in _collections.Values)
            {
                foreach (var record in collection.Values)
                {
                    var similarity = CosineSimilarity(vector, record.Vector);
                    results.Add(new VectorDbSearchResult
                    {
                        Id = record.Id,
                        Score = similarity,
                        Metadata = record.Metadata,
                        Collection = record.Collection
                    });
                }
            }

            var ordered = results.OrderByDescending(r => r.Score).Take(topK).ToList();
            return Task.FromResult<IReadOnlyList<VectorDbSearchResult>>(ordered);
        }

        #endregion

        #region Diagnostics

        /// <summary>
        /// Returns the total number of vectors stored in a given collection.
        /// </summary>
        public Task<int> GetVectorCountAsync(string collection, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException(nameof(collection));

            if (_collections.TryGetValue(collection, out var coll))
                return Task.FromResult(coll.Count);

            return Task.FromResult(0);
        }

        /// <summary>
        /// Returns a small randomized sample of records from the specified collection.
        /// </summary>
        public Task<IReadOnlyList<VectorDBRecord>> SampleVectorsAsync(
            string collection,
            int sampleCount,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException(nameof(collection));

            if (!_collections.TryGetValue(collection, out var coll) || coll.Count == 0)
                return Task.FromResult<IReadOnlyList<VectorDBRecord>>(Array.Empty<VectorDBRecord>());

            var random = new Random();
            var sample = coll.Values
                .OrderBy(_ => random.Next())
                .Take(Math.Min(sampleCount, coll.Count))
                .ToList();

            return Task.FromResult<IReadOnlyList<VectorDBRecord>>(sample);
        }

        #endregion

        #region Helpers

        private static float CosineSimilarity(float[] a, float[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
                return 0f;

            float dot = 0, magA = 0, magB = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                magA += a[i] * a[i];
                magB += b[i] * b[i];
            }

            return (float)(dot / (Math.Sqrt(magA) * Math.Sqrt(magB) + 1e-10));
        }

        #endregion
    }
}
