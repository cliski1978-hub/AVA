namespace AVA.Memory.Abstractions.Models
{
    /// <summary>
    /// Represents a scored result from a memory or vector search query.
    /// </summary>
    public class QueryHit
    {
        public MemoryRecordDto Record { get; set; } = default!;
        public double Score { get; set; }
    }
}
