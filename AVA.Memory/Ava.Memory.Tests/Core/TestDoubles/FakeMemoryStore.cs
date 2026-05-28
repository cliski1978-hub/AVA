using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Tests.Core.TestDoubles
{
    /// <summary>
    /// Simple in-memory test double for IMemoryStore.
    /// Used to simulate persistence without SQL dependency.
    /// </summary>
    internal class FakeMemoryStore : IMemoryStore
    {
        private readonly Dictionary<string, MemoryRecordDto> _records = new();

        public Task<string> UpsertAsync(MemoryRecordDto record, CancellationToken ct)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            if (string.IsNullOrWhiteSpace(record.ID))
                record.ID = Guid.NewGuid().ToString("N");

            record.UpdatedAt = DateTime.UtcNow;
            _records[record.ID] = record;

            return Task.FromResult(record.ID);
        }

        public Task<MemoryRecordDto?> GetAsync(string id, CancellationToken ct)
        {
            _records.TryGetValue(id, out var record);
            return Task.FromResult<MemoryRecordDto?>(record);
        }

        public Task<bool> DeleteAsync(string id, CancellationToken ct)
        {
            var removed = _records.Remove(id);
            return Task.FromResult(removed);
        }

        public Task<(IReadOnlyList<MemoryRecordDto> Items, int Total)> ListAsync(int skip, int take, CancellationToken ct)
        {
            var items = _records.Values
                .OrderByDescending(r => r.CreatedAt)
                .Skip(skip)
                .Take(take)
                .ToList();

            var total = _records.Count;
            return Task.FromResult<(IReadOnlyList<MemoryRecordDto>, int)>((items, total));
        }

        public Task ClearAsync(CancellationToken ct)
        {
            _records.Clear();
            return Task.CompletedTask;
        }
    }
}
