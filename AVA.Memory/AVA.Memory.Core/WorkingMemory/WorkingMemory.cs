using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Core.WorkingMemory
{
    /// <summary>
    /// In-memory implementation of IWorkingMemory used by AVA Core.
    /// Maintains transient records with optional TTL expiration.
    /// </summary>
    public class WorkingMemory : IWorkingMemory
    {
        private readonly ConcurrentDictionary<string, (MemoryRecordDto Record, DateTime Expiry)> _cache
            = new ConcurrentDictionary<string, (MemoryRecordDto, DateTime)>();

        /// <summary>
        /// Returns all currently active items (not expired).
        /// </summary>
        public Task<IReadOnlyList<MemoryRecordDto>> GetItemsAsync(CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var items = _cache.Values
                .Where(v => v.Expiry > now)
                .Select(v => v.Record)
                .ToList()
                .AsReadOnly();

            return Task.FromResult<IReadOnlyList<MemoryRecordDto>>(items);
        }

        /// <summary>
        /// Adds or refreshes a memory record in working memory with a TTL.
        /// </summary>
        public Task AddOrRefreshAsync(MemoryRecordDto record, TimeSpan ttl, CancellationToken ct)
        {
            var expiry = DateTime.UtcNow.Add(ttl);
            _cache[record.ID] = (record, expiry);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a record by ID.
        /// </summary>
        public Task RemoveAsync(string id, CancellationToken ct)
        {
            _cache.TryRemove(id, out _);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Flushes all records from working memory.
        /// </summary>
        public Task FlushAsync(CancellationToken ct)
        {
            _cache.Clear();
            return Task.CompletedTask;
        }
    }
}
