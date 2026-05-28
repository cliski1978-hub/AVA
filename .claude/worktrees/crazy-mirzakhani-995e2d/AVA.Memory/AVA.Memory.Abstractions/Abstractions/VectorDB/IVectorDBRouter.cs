using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;

namespace AVA.Memory.Abstractions.VectorDB
{
    /// <summary>
    /// Determines routing of vector records across multiple collections in the VectorDB backend.
    /// The router is responsible for deciding which semantic domain (collection) a record
    /// belongs to based on its topic, metadata, or contextual similarity.
    /// 
    /// The router also supports moving records between collections when topics are
    /// reclassified or overridden by higher-order reasoning.
    /// </summary>
    public interface IVectorDBRouter
    {
        #region Resolution

        /// <summary>
        /// Resolves the name of the collection where a record should be stored.
        /// </summary>
        /// <param name="record">The vector record to route.</param>
        /// <returns>The target collection name (e.g., "automation_memory").</returns>
        string GetTargetCollection(VectorDBRecord record);

        #endregion

        #region Migration

        /// <summary>
        /// Moves a record from one collection to another when topic reclassification occurs.
        /// Implementations should handle deletion from the source collection and
        /// insertion into the destination collection atomically if possible.
        /// </summary>
        /// <param name="record">The record to move.</param>
        /// <param name="newCollection">The target collection name.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task MoveRecordAsync(VectorDBRecord record, string newCollection, CancellationToken ct);

        #endregion
    }
}
