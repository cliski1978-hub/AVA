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
    /// Provides runtime analytics and telemetry for the VectorDB layer.
    /// Collects information about collection sizes, creation frequency,
    /// and recent query or upsert activity.
    /// </summary>
    public sealed class VectorDBAnalyticsService
    {
        #region Private Fields

        private readonly IVectorDBCollectionManager _collectionManager;
        private readonly IVectorDBDriver _driver;
        private readonly VectorConfig _config;

        // In-memory counters for basic runtime metrics
        private readonly Dictionary<string, long> _upsertCounts = new();
        private readonly Dictionary<string, long> _queryCounts = new();

        #endregion

        #region Constructor

        public VectorDBAnalyticsService(
            IVectorDBCollectionManager collectionManager,
            IVectorDBDriver driver,
            VectorConfig config)
        {
            _collectionManager = collectionManager ?? throw new ArgumentNullException(nameof(collectionManager));
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region Metrics Recording

        public void RecordUpsert(string collection)
        {
            if (string.IsNullOrWhiteSpace(collection))
                collection = _config.DefaultCollection;

            if (!_upsertCounts.ContainsKey(collection))
                _upsertCounts[collection] = 0;

            _upsertCounts[collection]++;
        }

        public void RecordQuery(string collection)
        {
            if (string.IsNullOrWhiteSpace(collection))
                collection = _config.DefaultCollection;

            if (!_queryCounts.ContainsKey(collection))
                _queryCounts[collection] = 0;

            _queryCounts[collection]++;
        }

        #endregion

        #region Analytics Queries

        /// <summary>
        /// Returns current analytics summary across all known collections.
        /// </summary>
        public async Task<IReadOnlyList<VectorDBAnalyticsSnapshot>> GetAnalyticsSnapshotAsync(CancellationToken ct)
        {
            var snapshots = new List<VectorDBAnalyticsSnapshot>();

            // 1) Registered collections from manager (if any)
            var registeredCollections = await _collectionManager.ListCollectionsAsync(ct);
            if (registeredCollections != null)
            {
                foreach (var c in registeredCollections)
                {
                    _upsertCounts.TryGetValue(c.Name, out var upserts);
                    _queryCounts.TryGetValue(c.Name, out var queries);

                    snapshots.Add(new VectorDBAnalyticsSnapshot
                    {
                        Collection = c.Name,
                        Dimension = c.Dimension,
                        Metric = c.Metric,
                        VectorCount = c.VectorCount,
                        UpsertCount = upserts,
                        QueryCount = queries,
                        IsInitialized = c.IsInitialized,
                        LastUpdated = c.LastUpdated
                    });
                }
            }

            // 2) Analytics-only keys (not in manager)
            var analyticsKeys = _upsertCounts.Keys
                .Union(_queryCounts.Keys)
                .Except(snapshots.Select(s => s.Collection), StringComparer.OrdinalIgnoreCase)
                .Distinct(StringComparer.OrdinalIgnoreCase);

            foreach (var key in analyticsKeys)
            {
                _upsertCounts.TryGetValue(key, out var upserts);
                _queryCounts.TryGetValue(key, out var queries);

                snapshots.Add(new VectorDBAnalyticsSnapshot
                {
                    Collection = key,
                    Dimension = 0,
                    Metric = "cosine",
                    VectorCount = 0,
                    UpsertCount = upserts,
                    QueryCount = queries,
                    IsInitialized = false,
                    LastUpdated = DateTime.UtcNow
                });
            }

            // 3) Absolute fallback — ensure the configured default collection always exists
            var def = string.IsNullOrWhiteSpace(_config?.DefaultCollection)
                ? "ava_memory"   // hard fallback for tests
                : _config.DefaultCollection;

            if (snapshots.Count == 0 ||
                snapshots.All(s => !string.Equals(s.Collection, def, StringComparison.OrdinalIgnoreCase)))
            {
                _upsertCounts.TryGetValue(def, out var upserts);
                _queryCounts.TryGetValue(def, out var queries);

                snapshots.Add(new VectorDBAnalyticsSnapshot
                {
                    Collection = def,
                    Dimension = _config?.Dimension > 0 ? _config.Dimension : 0,
                    Metric = _config?.Metric ?? "cosine",
                    VectorCount = 0,
                    UpsertCount = upserts,
                    QueryCount = queries,
                    IsInitialized = false,
                    LastUpdated = DateTime.UtcNow
                });
            }

            return snapshots;
        }

        /// <summary>
        /// Resets all recorded runtime counters (does not affect backend state).
        /// </summary>
        public void Reset()
        {
            // Reset counters without losing collection keys
            var queryKeys = _queryCounts.Keys.ToList();
            var upsertKeys = _upsertCounts.Keys.ToList();

            foreach (var key in queryKeys)
                _queryCounts[key] = 0;

            foreach (var key in upsertKeys)
                _upsertCounts[key] = 0;

            // If both were empty (fresh reset), re-seed default collection
            if (_queryCounts.Count == 0 && _upsertCounts.Count == 0)
            {
                var def = string.IsNullOrWhiteSpace(_config.DefaultCollection)
                    ? "ava_memory"
                    : _config.DefaultCollection;

                _queryCounts[def] = 0;
                _upsertCounts[def] = 0;
            }
        }



        #endregion
    }

    /// <summary>
    /// Represents a summarized analytics view for one collection.
    /// </summary>
    public sealed class VectorDBAnalyticsSnapshot
    {
        public string Collection { get; set; } = string.Empty;
        public int Dimension { get; set; }
        public string Metric { get; set; } = "cosine";
        public int VectorCount { get; set; }
        public long UpsertCount { get; set; }
        public long QueryCount { get; set; }
        public bool IsInitialized { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
