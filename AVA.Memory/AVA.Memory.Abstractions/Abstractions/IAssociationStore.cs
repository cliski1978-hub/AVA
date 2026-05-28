using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Abstractions
{
    /// <summary>
    /// Defines the contract for managing association edges between memory records.
    /// Works purely with DTOs for cross-framework compatibility.
    /// </summary>
    public interface IAssociationStore
    {
        /// <summary>
        /// Inserts or updates an association edge and returns its ID.
        /// </summary>
        Task<string> UpsertAsync(AssociationEdgeDto edge, CancellationToken ct);

        /// <summary>
        /// Retrieves an association edge by ID.
        /// </summary>
        Task<AssociationEdgeDto?> GetAsync(string id, CancellationToken ct);

        /// <summary>
        /// Deletes an association edge by ID and returns success flag.
        /// </summary>
        Task<bool> DeleteAsync(string id, CancellationToken ct);

        /// <summary>
        /// Lists association edges with pagination.
        /// </summary>
        Task<(IReadOnlyList<AssociationEdgeDto> Items, int Total)> ListAsync(int skip, int take, CancellationToken ct);
    }
}
