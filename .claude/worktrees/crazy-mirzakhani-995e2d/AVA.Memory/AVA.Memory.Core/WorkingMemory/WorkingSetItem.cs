namespace AVA.Memory.Core.WorkingMemory
{
    using AVA.Memory.Core.Models;
    using System;

    /// <summary>
    /// Represents a single entry in the working memory buffer.
    /// </summary>
    public class WorkingSetItem
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// The text content or cue being held in working memory.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Optional embedding vector for re-query or similarity.
        /// </summary>
        public float[] Vector { get; set; }

        /// <summary>
        /// Priority level (low/normal/high/critical).
        /// </summary>
        public WorkingSetPriority Priority { get; set; } = WorkingSetPriority.Normal;

        /// <summary>
        /// Expiration timestamp (UTC). If past this time, the item is considered expired.
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// When the item was inserted into the working set.
        /// </summary>
        public DateTime InsertedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Source of the item (e.g. "user", "retriever", "agent").
        /// </summary>
        public string Source { get; set; }
    }
}
