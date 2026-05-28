using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;

namespace AVA.Memory.Abstractions.VectorDB
{
    /// <summary>
    /// Defines lifecycle management operations for VectorDB collections.
    /// Responsible for ensuring that collections exist in both the backend driver
    /// and the persistent registry. 
    /// 
    /// Acts as a bridge between <see cref="IVectorDBCollectionRegistry"/> 
    /// and the backend <see cref="IVectorDBDriver"/>.
    /// </summary>
    public interface IVectorDBCollectionManager
    {
        #region Creation and Existence

        /// <summary>
        /// Ensures the specified collection exists in both the registry and the VectorDB backend.
        /// If missing, it will be created with the specified configuration.
        /// </summary>
        Task<bool> CreateIfNotExistsAsync(VectorDBCollectionDto collection, CancellationToken ct);

        /// <summary>
        /// Checks whether a collection exists in the backend driver.
        /// </summary>
        Task<bool> ExistsAsync(string name, CancellationToken ct);

        #endregion

        #region Listing

        /// <summary>
        /// Returns all collections known to the VectorDB backend or registry.
        /// Used for diagnostics, API responses, and synchronization.
        /// </summary>
        Task<IReadOnlyList<VectorDBCollectionDto>> ListCollectionsAsync(CancellationToken ct);

        #endregion

        #region Deletion

        /// <summary>
        /// Deletes the specified collection from the backend VectorDB and removes it from the registry.
        /// </summary>
        Task<bool> DeleteAsync(string name, CancellationToken ct);

        #endregion

        #region Synchronization

        /// <summary>
        /// Synchronizes the registry with the backend driver.
        /// Ensures both systems reflect the same collection set and metadata.
        /// </summary>
        Task<bool> SyncAsync(CancellationToken ct);

        #endregion
    }
}
