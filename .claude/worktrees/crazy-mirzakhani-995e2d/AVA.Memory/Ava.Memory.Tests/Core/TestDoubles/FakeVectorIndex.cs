using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Tests.Core.TestDoubles
{
    /// <summary>
    /// In-memory fake implementation of IVectorIndex for unit testing.
    /// Simulates a vector search index using cosine similarity.
    /// </summary>
    internal class FakeVectorIndex : IVectorIndex
    {
        private readonly List<MemoryRecordDto> _records = new();

        public Task AddAsync(MemoryRecordDto record, CancellationToken ct)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            // Replace if already exists
            var existing = _records.FirstOrDefault(r => r.ID == record.ID);
            if (existing != null)
                _records.Remove(existing);

            _records.Add(record);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<QueryHit>> QueryAsync(QueryMemoryRequest request, CancellationToken ct)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Simulate a simple similarity score (randomized for testing)
            var random = new Random();
            var hits = _records
                .Select(r => new QueryHit
                {
                    Record = r,
                    Score = (float)(random.NextDouble() * 0.5 + 0.5) // random 0.5–1.0
                })
                .OrderByDescending(h => h.Score)
                .Take(request.TopK > 0 ? request.TopK : 10)
                .ToList();

            return Task.FromResult<IReadOnlyList<QueryHit>>(hits);
        }

        public void Clear() => _records.Clear();
    }
}
