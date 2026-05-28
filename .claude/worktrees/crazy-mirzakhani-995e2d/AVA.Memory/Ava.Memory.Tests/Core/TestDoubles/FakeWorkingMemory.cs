using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Tests.Core.TestDoubles
{
    /// <summary>
    /// Simple in-memory fake implementation of IWorkingMemory for testing.
    /// Supports TTL expiration and recency ordering.
    /// </summary>
    internal class FakeWorkingMemory : IWorkingMemory
    {
        private readonly Dictionary<string, (MemoryRecordDto Record, DateTime Expiry)> _records = new();
        private readonly object _lock = new();

        public Task AddOrRefreshAsync(MemoryRecordDto record, TimeSpan ttl, CancellationToken ct)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            lock (_lock)
            {
                _records[record.ID] = (record, DateTime.UtcNow.Add(ttl));
            }

            return Task.CompletedTask;
        }

        public Task RemoveAsync(string id, CancellationToken ct)
        {
            lock (_lock)
            {
                _records.Remove(id);
            }

            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<MemoryRecordDto>> GetItemsAsync(CancellationToken ct)
        {
            List<MemoryRecordDto> items;
            lock (_lock)
            {
                var now = DateTime.UtcNow;
                // Drop expired items
                var expired = _records
                    .Where(kv => kv.Value.Expiry < now)
                    .Select(kv => kv.Key)
                    .ToList();

                foreach (var key in expired)
                    _records.Remove(key);

                items = _records.Values
                    .Select(v => v.Record)
                    .OrderByDescending(r => r.UpdatedAt)
                    .ToList();
            }

            return Task.FromResult<IReadOnlyList<MemoryRecordDto>>(items);
        }

        public Task FlushAsync(CancellationToken ct)
        {
            lock (_lock)
            {
                _records.Clear();
            }

            return Task.CompletedTask;
        }

        public void Clear()
        {
            lock (_lock)
            {
                _records.Clear();
            }
        }
    }
}
