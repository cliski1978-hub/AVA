using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Models;

namespace Ava.Memory.Tests.Core.Utilities
{
    /// <summary>
    /// Lightweight in-memory working memory buffer for tests.
    /// Implements FIFO eviction, TTL refresh, and capacity bounds.
    /// </summary>
    public sealed class TestWorkingMemory : IWorkingMemory
    {
        private readonly LinkedList<MemoryRecordDto> _records = new();
        private readonly int _capacity;

        public TestWorkingMemory(int capacity = 3)
        {
            if (capacity <= 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
            _capacity = capacity;
        }

        /// <summary>
        /// Adds or refreshes a record with a TTL. 
        /// Evicts the oldest if at capacity.
        /// </summary>
        public Task AddOrRefreshAsync(MemoryRecordDto record, TimeSpan ttl, CancellationToken ct = default)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            record.UpdatedAt = DateTime.UtcNow;
            record.LastAccessedAt = record.UpdatedAt;

            lock (_records)
            {
                // Remove if already exists (refresh)
                var existing = _records.FirstOrDefault(r => r.ID == record.ID);
                if (existing != null)
                    _records.Remove(existing);

                // Evict oldest if full BEFORE adding new
                if (_records.Count >= _capacity)
                    _records.RemoveFirst();

                _records.AddLast(record);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns all items in newest-first order.
        /// </summary>
        public Task<IReadOnlyList<MemoryRecordDto>> GetItemsAsync(CancellationToken ct = default)
        {
            lock (_records)
            {
                var ordered = _records.Reverse().ToList();
                return Task.FromResult((IReadOnlyList<MemoryRecordDto>)ordered);
            }
        }

        /// <summary>
        /// Removes a record by ID if it exists.
        /// </summary>
        public Task RemoveAsync(string id, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                return Task.CompletedTask;

            lock (_records)
            {
                var existing = _records.FirstOrDefault(r => r.ID == id);
                if (existing != null)
                    _records.Remove(existing);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Clears all records.
        /// </summary>
        public Task FlushAsync(CancellationToken ct = default)
        {
            lock (_records)
            {
                _records.Clear();
            }

            return Task.CompletedTask;
        }

        // Convenience property for debugging
        public int Count
        {
            get
            {
                lock (_records)
                {
                    return _records.Count;
                }
            }
        }
    }
}
