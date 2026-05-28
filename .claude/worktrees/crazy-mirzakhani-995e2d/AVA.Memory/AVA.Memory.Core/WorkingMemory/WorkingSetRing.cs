namespace AVA.Memory.Core.WorkingMemory
{
    using AVA.Memory.Core.Models;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Circular buffer structure for holding a fixed number of working memory items.
    /// Supports TTL and priority-based eviction.
    /// </summary>
    public class WorkingSetRing
    {
        private readonly WorkingSetItem[] _buffer;
        private int _head = 0;
        private int _count = 0;

        public int Capacity { get; }

        public WorkingSetRing(int capacity = 50)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            Capacity = capacity;
            _buffer = new WorkingSetItem[capacity];
        }

        public int Count => _count;

        /// <summary>
        /// Add a new item, evicting expired or lowest-priority items if necessary.
        /// </summary>
        public void Add(WorkingSetItem item)
        {
            // Purge any expired items inline
            PurgeExpired();

            if (_count < Capacity)
            {
                _buffer[_head] = item;
                _head = (_head + 1) % Capacity;
                _count++;
            }
            else
            {
                // Evict candidate: lowest-priority & oldest
                int evictIndex = FindEvictionCandidate();
                _buffer[evictIndex] = item;
                _head = (evictIndex + 1) % Capacity;
            }
        }

        public IEnumerable<WorkingSetItem> GetRecent()
        {
            for (int i = 0; i < _count; i++)
            {
                int idx = (_head - 1 - i + Capacity) % Capacity;
                var item = _buffer[idx];
                if (item != null && item.ExpiresAt > DateTime.UtcNow)
                {
                    yield return item;
                }
            }
        }

        public void PurgeExpired()
        {
            var now = DateTime.UtcNow;
            for (int i = 0; i < Capacity; i++)
            {
                var item = _buffer[i];
                if (item != null && item.ExpiresAt <= now && item.Priority != WorkingSetPriority.Critical)
                {
                    _buffer[i] = null;
                    _count--;
                }
            }
        }

        private int FindEvictionCandidate()
        {
            int? lowestIdx = null;
            WorkingSetItem lowest = null;

            for (int i = 0; i < Capacity; i++)
            {
                var item = _buffer[i];
                if (item == null) return i; // empty slot
                if (item.Priority == WorkingSetPriority.Critical) continue;

                if (lowest == null ||
                    item.Priority < lowest.Priority ||
                    (item.Priority == lowest.Priority && item.InsertedAt < lowest.InsertedAt))
                {
                    lowest = item;
                    lowestIdx = i;
                }
            }

            return lowestIdx ?? _head;
        }
    }
}
