namespace AVA.Memory.Abstractions.VectorDB
{
    /// <summary>
    /// Exposes collection-level statistics and performance diagnostics
    /// for observability and health monitoring.
    /// </summary>
    public interface IVectorDBAnalytics
    {
        Task<IDictionary<string, object>> GetCollectionStatsAsync(string collection, CancellationToken ct = default);
        Task<IDictionary<string, object>> GetSystemMetricsAsync(CancellationToken ct = default);
    }
}
