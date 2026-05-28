namespace AVA.Memory.Abstractions.VectorDB
{
    /// <summary>
    /// Defines background maintenance operations for VectorDB collections,
    /// including pruning, drift detection, and archival.
    /// </summary>
    public interface IVectorDBMaintenance
    {
        Task<int> PruneDuplicatesAsync(string collection, float similarityThreshold, CancellationToken ct = default);
        Task<int> ArchiveStaleRecordsAsync(string collection, TimeSpan maxAge, CancellationToken ct = default);
        Task<int> RebalanceCollectionsAsync(CancellationToken ct = default);
    }
}
