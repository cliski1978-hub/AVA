using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Abstractions
{
    /// <summary>
    /// Defines a contract for retrieval operations from memory stores or vector indices.
    /// </summary>
    public interface IRetriever
    {
        /// <summary>
        /// Retrieves records from memory based on a semantic or metadata query.
        /// </summary>
        /// <param name="request">Query definition including embedding, tags, and filters.</param>
        /// <param name="ct">Cancellation token for cooperative cancellation.</param>
        /// <returns>List of query hits ranked by similarity score.</returns>
        Task<IReadOnlyList<QueryHit>> RetrieveAsync(QueryMemoryRequest request, CancellationToken ct);
    }
}
