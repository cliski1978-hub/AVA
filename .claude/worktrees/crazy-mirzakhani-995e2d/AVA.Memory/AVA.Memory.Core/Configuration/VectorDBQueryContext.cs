using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;
using AVA.Memory.Core.Configuration;

namespace AVA.Memory.Core.Services
{
    /// <summary>
    /// Encapsulates the full state and parameters of a VectorDB semantic query.
    /// Includes routing, vector payload, filters, and result handling.
    /// </summary>
    public sealed class VectorDBQueryContext
    {
        #region Core Properties

        /// <summary>
        /// Optional unique ID for tracing this query through logs and metrics.
        /// </summary>
        public string QueryId { get; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// The input vector embedding used for similarity search.
        /// </summary>
        public float[] Vector { get; }

        /// <summary>
        /// Optional human-readable query text (used for embedding caching or diagnostics).
        /// </summary>
        public string? Text { get; }

        /// <summary>
        /// Maximum number of results to return.
        /// </summary>
        public int TopK { get; }

        /// <summary>
        /// Optional metadata filter or tag constraint.
        /// </summary>
        public string? Filter { get; }

        /// <summary>
        /// The logical target collection name resolved by the router.
        /// </summary>
        public string TargetCollection { get; }

        /// <summary>
        /// Timestamp when this query context was created.
        /// </summary>
        public DateTime CreatedUtc { get; } = DateTime.UtcNow;

        #endregion

        #region Dependencies

        private readonly IVectorDBDriver _driver;
        private readonly IVectorDBRouter _router;
        private readonly VectorConfig _config;

        #endregion

        #region Constructor

        public VectorDBQueryContext(
            float[] vector,
            IVectorDBDriver driver,
            IVectorDBRouter router,
            VectorConfig config,
            string? text = null,
            string? filter = null,
            int topK = 8)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            Vector = vector ?? throw new ArgumentNullException(nameof(vector));
            Text = text;
            Filter = filter;
            TopK = topK > 0 ? topK : 8;

            // Let router decide where this vector belongs semantically
            TargetCollection = _router.GetTargetCollection(new VectorDBRecord
            {
                Id = QueryId,
                Vector = vector,
                Metadata = new Dictionary<string, object>
                {
                    { "context_type", "query" },
                    { "filter", filter ?? string.Empty }
                }
            });
        }

        #endregion

        #region Execution

        /// <summary>
        /// Executes the vector similarity search against the resolved collection.
        /// </summary>
        public async Task<IReadOnlyList<VectorDbSearchResult>> ExecuteAsync(CancellationToken ct)
        {
            // Future: driver may accept a collection override parameter
            var results = await _driver.SearchAsync(Vector, TopK, Filter);

            // Decorate with context metadata
            foreach (var r in results)
            {
                r.Metadata ??= new Dictionary<string, object>();
                r.Metadata["collection"] = TargetCollection;
                r.Metadata["query_id"] = QueryId;
            }

            return results;
        }

        #endregion

        #region Diagnostics

        /// <summary>
        /// Returns a summary string for quick logging.
        /// </summary>
        public override string ToString()
        {
            return $"[QueryContext {QueryId}] {TopK} results | Filter={Filter ?? "none"} | Collection={TargetCollection}";
        }

        #endregion
    }
}
