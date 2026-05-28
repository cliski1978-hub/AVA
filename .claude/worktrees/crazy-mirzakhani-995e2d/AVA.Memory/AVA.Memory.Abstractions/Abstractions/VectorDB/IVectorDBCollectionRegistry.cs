using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;

namespace AVA.Memory.Abstractions.VectorDB
{
    /// <summary>
    /// Defines the persistent registry for all VectorDB collections managed by AVA.
    /// The registry maintains metadata describing each collection’s semantic domain,
    /// creation time, vector count, and centroid statistics.
    /// 
    /// Implementations may persist registry data in JSON, SQL, or distributed metadata stores.
    /// </summary>
    public interface IVectorDBCollectionRegistry
    {
        #region Query

        /// <summary>
        /// Lists all known vector collections currently registered in the memory system.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>Read-only list of vector collections known to the registry.</returns>
        Task<IReadOnlyList<VectorDBCollectionDto>> ListAsync(CancellationToken ct);

        /// <summary>
        /// Retrieves a collection profile by its unique name.
        /// </summary>
        /// <param name="name">The name of the collection (e.g. "automation_memory").</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The matching <see cref="VectorDBCollection"/> if found, otherwise null.</returns>
        Task<VectorDBCollectionDto?> GetAsync(string name, CancellationToken ct);

        #endregion

        #region Registration

        /// <summary>
        /// Adds a new collection to the registry or updates an existing one.
        /// Implementations should update timestamps and metadata consistency.
        /// </summary>
        /// <param name="collection">The collection metadata object.</param>
        /// <param name="ct">Cancellation token.</param>
        Task RegisterOrUpdateAsync(VectorDBCollectionDto collection, CancellationToken ct);

        /// <summary>
        /// Removes a collection record from the registry.
        /// This does not delete the collection in the VectorDB backend.
        /// </summary>
        /// <param name="name">The name of the collection to remove.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if removed successfully, false if not found.</returns>
        Task<bool> RemoveAsync(string name, CancellationToken ct);

        #endregion

        #region Synchronization

        /// <summary>
        /// Updates registry metadata based on current state in the VectorDB backend.
        /// Implementations may call driver-level APIs to refresh collection data.
        /// </summary>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True if the registry was updated successfully.</returns>
        Task<bool> SyncWithBackendAsync(CancellationToken ct);

        #endregion
    }
}
