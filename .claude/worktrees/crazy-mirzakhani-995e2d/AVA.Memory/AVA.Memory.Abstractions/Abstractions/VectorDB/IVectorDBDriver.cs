using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;

namespace AVA.Memory.Abstractions.VectorDB
{
    /// <summary>
    /// Defines the standard contract for persistent VectorDB drivers.
    /// Implementations may include Qdrant, Milvus, Chroma, or other
    /// distributed vector database backends.
    /// 
    /// This interface is distinct from <see cref="Indexing.IVectorIndex"/>,
    /// which handles in-memory or SQL-based indexing.
    /// </summary>
    public interface IVectorDBDriver
    {
        #region Collection Management

        /// <summary>
        /// Ensures the specified collection exists in the backend.
        /// If missing, the driver should create it with the given parameters.
        /// </summary>
        Task<bool> EnsureCollectionAsync(VectorDBCollectionDto collection, CancellationToken ct = default);

        /// <summary>
        /// Lists all collections currently defined in the backend.
        /// </summary>
        Task<IReadOnlyList<VectorDBCollectionDto>> ListCollectionsAsync(CancellationToken ct = default);

        #endregion

        #region CRUD Operations

        /// <summary>
        /// Inserts or updates a single vector record within a collection.
        /// The collection name is provided in the record’s metadata or configuration context.
        /// </summary>
        Task UpsertAsync(VectorDBRecord record, CancellationToken ct = default);

        /// <summary>
        /// Deletes a vector record by its unique identifier.
        /// </summary>
        Task DeleteAsync(string id, string? collection = null, CancellationToken ct = default);

        /// <summary>
        /// Deletes an entire collection and its contents from the backend.
        /// </summary>
        Task<bool> DeleteCollectionAsync(string name, CancellationToken ct = default);

        #endregion

        #region Search

        /// <summary>
        /// Performs a nearest-neighbor search using the provided vector.
        /// Returns up to <paramref name="topK"/> matching results ordered by similarity score.
        /// Optional <paramref name="filter"/> allows restricting by metadata tags.
        /// </summary>
        Task<IReadOnlyList<VectorDbSearchResult>> SearchAsync(
            float[] vector,
            int topK,
            string? filter = null,
            CancellationToken ct = default
        );

        #endregion

        #region Diagnostics

        /// <summary>
        /// Retrieves the total number of vectors currently stored in a given collection.
        /// Used for maintenance verification and consistency analysis.
        /// </summary>
        Task<int> GetVectorCountAsync(string collection, CancellationToken ct = default);

        /// <summary>
        /// Samples a small set of vectors from the specified collection
        /// to verify dimension consistency and embedding validity.
        /// </summary>
        Task<IReadOnlyList<VectorDBRecord>> SampleVectorsAsync(
            string collection,
            int sampleCount,
            CancellationToken ct = default);

        #endregion
    }
}
