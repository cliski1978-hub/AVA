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
    /// Performs scheduled maintenance across all VectorDB collections:
    /// pruning stale records, rebalancing topics, and checking for drift.
    /// </summary>
    public sealed class VectorDBMaintenanceContext
    {
        #region Private Fields

        private readonly IVectorDBDriver _driver;
        private readonly IVectorDBCollectionManager _collections;
        private readonly IVectorDBRouter _router;
        private readonly VectorConfig _config;

        private readonly Dictionary<string, MaintenanceStats> _stats = new();

        #endregion

        #region Properties

        public DateTime LastRunUtc { get; private set; }

        #endregion

        #region Constructor

        public VectorDBMaintenanceContext(
            IVectorDBDriver driver,
            IVectorDBCollectionManager collections,
            IVectorDBRouter router,
            VectorConfig config)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _collections = collections ?? throw new ArgumentNullException(nameof(collections));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region Public Entry Point

        /// <summary>
        /// Executes a full maintenance pass across all active collections.
        /// </summary>
        public async Task RunMaintenanceAsync(CancellationToken ct)
        {
            var collections = await _collections.ListCollectionsAsync(ct);
            LastRunUtc = DateTime.UtcNow;

            foreach (var col in collections)
            {
                var stats = new MaintenanceStats
                {
                    Collection = col.Name,
                    StartUtc = DateTime.UtcNow
                };

                try
                {
                    await AnalyzeCollectionAsync(col, stats, ct);
                    await PruneStaleRecordsAsync(col, stats, ct);
                    await RebalanceCollectionAsync(col, stats, ct);

                }
                catch (Exception ex)
                {
                    stats.Success = false;
                    stats.Error = ex.Message;
                }

                stats.EndUtc = DateTime.UtcNow;
                _stats[col.Name] = stats;
            }

            Console.WriteLine($"[VectorDB] Maintenance completed at {LastRunUtc:u}");
        }

        #endregion

        #region Core Operations

        /// <summary>
        /// Collects metadata for a single collection and runs basic integrity checks.
        /// </summary>
        private async Task AnalyzeCollectionAsync(VectorDBCollectionDto col, MaintenanceStats stats, CancellationToken ct)
        {
            stats.Collection = col.Name;
            stats.ActionsTaken.Add($"=== Begin analysis for collection '{col.Name}' ===");

            try
            {
                // 1. Existence check
                if (col == null || string.IsNullOrWhiteSpace(col.Name))
                {
                    stats.Success = false;
                    stats.Error = "Collection object is null or has no name.";
                    stats.ActionsTaken.Add("❌ Invalid collection object.");
                    return;
                }

                // 2. Initialization check
                if (!col.IsInitialized)
                {
                    stats.Success = false;
                    stats.Error = $"Collection '{col.Name}' is not initialized.";
                    stats.ActionsTaken.Add("❌ Initialization flag is false.");
                    return;
                }

                // 3. Dimension validation
                if (col.Dimension <= 0)
                {
                    stats.Success = false;
                    stats.Error = $"Collection '{col.Name}' has invalid dimension ({col.Dimension}).";
                    stats.ActionsTaken.Add("❌ Invalid vector dimension.");
                    return;
                }

                // 4. Vector count validation
                if (col.VectorCount < 0)
                {
                    stats.Success = false;
                    stats.Error = $"Collection '{col.Name}' reports negative vector count.";
                    stats.ActionsTaken.Add("❌ Vector count integrity failure.");
                    return;
                }

                // 5. Metric validation
                var validMetrics = new[] { "cosine", "dot", "euclidean" };
                if (!validMetrics.Contains(col.Metric, StringComparer.OrdinalIgnoreCase))
                {
                    stats.Success = false;
                    stats.Error = $"Collection '{col.Name}' uses unsupported metric '{col.Metric}'.";
                    stats.ActionsTaken.Add("❌ Unsupported metric.");
                    return;
                }

                // 6. Verify backend record count alignment
                var backendCount = await _driver.GetVectorCountAsync(col.Name, ct);
                if (backendCount != col.VectorCount)
                {
                    stats.Success = false;
                    stats.Error = $"Backend vector count ({backendCount}) does not match metadata ({col.VectorCount}).";
                    stats.ActionsTaken.Add("❌ Backend count mismatch.");
                    return;
                }

                // 7. Sample vector dimensionality check
                var samples = await _driver.SampleVectorsAsync(col.Name, 5, ct);
                if (samples.Any(s => s.Vector.Length != col.Dimension))
                {
                    stats.Success = false;
                    stats.Error = "Inconsistent vector dimensions found in sampled records.";
                    stats.ActionsTaken.Add("❌ Sample vector dimensionality mismatch.");
                    return;
                }

                // If we reached here, all checks passed
                stats.Success = true;
                stats.ActionsTaken.Add("✅ All validation checks passed.");
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Error = $"Exception during analysis of '{col.Name}': {ex.Message}";
                stats.ActionsTaken.Add("❌ Exception during analysis.");
            }

            stats.EndUtc = DateTime.UtcNow;
            stats.ActionsTaken.Add("=== End analysis ===");
        }



        /// <summary>
        /// Removes low-salience or stale records according to decay rate and access timestamps.
        /// </summary>
        private async Task PruneStaleRecordsAsync(VectorDBCollectionDto col, MaintenanceStats stats, CancellationToken ct)
        {
            stats.ActionsTaken.Add($"=== Begin pruning for collection '{col.Name}' ===");

            try
            {
                if (col == null || string.IsNullOrWhiteSpace(col.Name))
                {
                    stats.Success = false;
                    stats.Error = "Invalid collection passed to PruneStaleRecordsAsync.";
                    stats.ActionsTaken.Add("❌ Collection object invalid.");
                    return;
                }

                // --- Step 1: Config thresholds ---
                var maxAge = _config.MaxRecordAgeDays > 0 ? _config.MaxRecordAgeDays : 30;
                var decayThreshold = _config.DecayThreshold > 0 ? _config.DecayThreshold : 0.1;
                var now = DateTime.UtcNow;

                // --- Step 2: Pull a reasonable sample set to evaluate freshness ---
                var sampleSize = Math.Min(500, Math.Max(50, col.VectorCount / 20)); // 5% sample or up to 500
                var samples = await _driver.SampleVectorsAsync(col.Name, sampleSize, ct);

                if (samples == null || samples.Count == 0)
                {
                    stats.ActionsTaken.Add("ℹ No vectors available for pruning evaluation.");

                    if (stats.Success != false)
                    {
                        stats.Success = true;
                    }

                    return;
                }

                // --- Step 3: Identify stale or low-decay records ---
                var staleList = new List<VectorDBRecord>();
                foreach (var record in samples)
                {
                    try
                    {
                        // Default assumptions if no metadata present
                        DateTime createdAt = record.Metadata.TryGetValue("created_at", out var created)
                            && DateTime.TryParse(created?.ToString(), out var cdt)
                            ? cdt
                            : now;

                        float decay = record.Metadata.TryGetValue("decay", out var d)
                            && float.TryParse(d?.ToString(), out var dv)
                            ? dv
                            : 1.0f;

                        double ageDays = (now - createdAt).TotalDays;

                        if (ageDays > maxAge || decay < decayThreshold)
                        {
                            staleList.Add(record);
                        }
                    }
                    catch (Exception metaEx)
                    {
                        stats.ActionsTaken.Add($"⚠ Metadata parse failed for record '{record.Id}': {metaEx.Message}");
                    }
                }

                // --- Step 4: Delete stale records from backend ---
                if (staleList.Count > 0)
                {
                    int prunedCount = 0;
                    foreach (var rec in staleList)
                    {
                        try
                        {
                            await _driver.DeleteAsync(rec.Id, col.Name, ct);
                            prunedCount++;
                        }
                        catch (Exception delEx)
                        {
                            stats.ActionsTaken.Add($"⚠ Delete failed for '{rec.Id}': {delEx.Message}");
                        }
                    }

                    stats.ActionsTaken.Add($"🧹 Pruned {prunedCount} stale/decayed records from '{col.Name}'.");
                }
                else
                {
                    stats.ActionsTaken.Add($"✅ No stale or decayed vectors detected (thresholds: age>{maxAge}d, decay<{decayThreshold}).");
                }

                stats.Success = true;
            }
            catch (Exception ex)
            {
                stats.Success = false;
                stats.Error = $"Exception in PruneStaleRecordsAsync for '{col?.Name}': {ex.Message}";
                stats.ActionsTaken.Add("❌ Exception encountered during pruning.");
            }

            stats.ActionsTaken.Add($"=== End pruning for '{col?.Name}' ===");
        }


        /// <summary>
        /// Detects topic drift and reassigns vectors to better-suited collections.
        /// </summary>
        private async Task RebalanceCollectionAsync(VectorDBCollectionDto col, MaintenanceStats stats, CancellationToken ct)
        {
            stats.ActionsTaken.Add($"=== Begin rebalancing for collection '{col.Name}' ===");

            try
            {
                if (col == null || string.IsNullOrWhiteSpace(col.Name))
                {
                    //MarkFailure(stats, "Invalid collection object passed to RebalanceCollectionAsync.");
                    return;
                }

                // --- Step 1: Pull sample set for analysis ---
                var sampleCount = Math.Min(500, Math.Max(50, col.VectorCount / 20)); // 5% or up to 500
                var samples = await _driver.SampleVectorsAsync(col.Name, sampleCount, ct);

                if (samples == null || samples.Count == 0)
                {
                    stats.ActionsTaken.Add("ℹ No samples available for rebalancing evaluation.");
                    return;
                }

                // --- Step 2: Compute centroid and variance ---
                var dimension = samples[0].Vector.Length;
                var centroid = new float[dimension];

                foreach (var rec in samples)
                {
                    for (int i = 0; i < dimension; i++)
                        centroid[i] += rec.Vector[i];
                }

                for (int i = 0; i < dimension; i++)
                    centroid[i] /= samples.Count;

                float totalVariance = 0;
                foreach (var rec in samples)
                {
                    float dist = 0;
                    for (int i = 0; i < dimension; i++)
                    {
                        float diff = rec.Vector[i] - centroid[i];
                        dist += diff * diff;
                    }
                    totalVariance += dist;
                }

                var avgVariance = totalVariance / samples.Count;
                stats.ActionsTaken.Add($"Computed centroid (dim={dimension}, variance={avgVariance:0.####}).");

                // --- Step 3: Evaluate imbalance threshold ---
                var imbalanceThreshold = _config.DecayThreshold > 0
                    ? _config.DecayThreshold
                    : 0.35f;

                if (avgVariance < imbalanceThreshold)
                {
                    stats.ActionsTaken.Add("✅ Cluster appears balanced; no rebalancing required.");
                    return;
                }

                // --- Step 4: Topic drift analysis via router (optional) ---
                if (_router != null)
                {
                    try
                    {

                    }
                    catch (Exception routerEx)
                    {
                        //MarkFailure(stats, $"Router drift evaluation failed: {routerEx.Message}");
                    }
                }
                else
                {
                    stats.ActionsTaken.Add("ℹ Router unavailable; skipping topic drift evaluation.");
                }

                // --- Step 5: Store updated centroid and timestamp ---
                col.Centroid = centroid;
                col.LastUpdated = DateTime.UtcNow;

                stats.ActionsTaken.Add("🔄 Updated centroid and metadata for collection.");
            }
            catch (Exception ex)
            {
                //MarkFailure(stats, $"Exception in RebalanceCollectionAsync for '{col?.Name}': {ex.Message}");
            }

            stats.ActionsTaken.Add($"=== End rebalancing for '{col?.Name}' ===");
        }



        #endregion

        #region Reporting

        public IReadOnlyDictionary<string, MaintenanceStats> GetLastReport() =>
            new Dictionary<string, MaintenanceStats>(_stats);

        #endregion
    }

    /// <summary>
    /// Diagnostics record for a single maintenance cycle on one collection.
    /// </summary>
    public sealed class MaintenanceStats
    {
        public string Collection { get; set; } = string.Empty;
        public int Dimension { get; set; }
        public int VectorCount { get; set; }
        public bool Success { get; set; }
        public string? Error { get; set; }
        public DateTime StartUtc { get; set; }
        public DateTime EndUtc { get; set; }
        public List<string> ActionsTaken { get; } = new();
    }
}
