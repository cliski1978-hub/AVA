using System;

namespace AVA.Memory.Abstractions.Models
{
    /// <summary>
    /// Represents a snapshot of the AVA memory system — including
    /// working memory, SQL persistence, and eviction statistics.
    /// </summary>
    public class MemoryStatsDto
    {
        /// <summary>
        /// Total number of active items in working memory.
        /// </summary>
        public int WorkingCount { get; set; }

        /// <summary>
        /// Total number of items stored in SQL-backed persistent memory.
        /// </summary>
        public int SqlCount { get; set; }

        /// <summary>
        /// Total number of expired items evicted since service start.
        /// </summary>
        public int ExpiredEvicted { get; set; }

        /// <summary>
        /// Total number of least-recently-used items evicted since service start.
        /// </summary>
        public int LruEvicted { get; set; }

        /// <summary>
        /// Timestamp of the oldest record currently in working memory.
        /// </summary>
        public DateTimeOffset? Oldest { get; set; }

        /// <summary>
        /// Timestamp of the newest record currently in working memory.
        /// </summary>
        public DateTimeOffset? Newest { get; set; }

        /// <summary>
        /// Time the monitor last ran and updated stats.
        /// </summary>
        public DateTimeOffset LastChecked { get; set; }

        /// <summary>
        /// Whether real-time streaming of memory stats is currently enabled.
        /// </summary>
        public bool StreamingEnabled { get; set; }

  
    }
}
