using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Abstractions
{
    /// <summary>
    /// Defines the contract for a vector search provider.
    /// Performs similarity-based queries across memory records.
    /// </summary>
    public interface IVectorIndex
    {
        /// <summary>
        /// Adds a memory record to the vector index.
        /// </summary>
        /// <param name="record">The record to add to the index.</param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        Task AddAsync(MemoryRecordDto record, CancellationToken ct);

        /// <summary>
        /// Performs a similarity search based on the provided query.
        /// Returns a list of ranked results with scores.
        /// </summary>
        /// <param name="request">Query definition including embedding and filters.</param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        Task<IReadOnlyList<QueryHit>> QueryAsync(QueryMemoryRequest request, CancellationToken ct);
    }
}
