using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;
using AVA.Memory.Core.Configuration;

namespace AVA.Memory.Core.Services
{
    /// <summary>
    /// Core orchestrator for AVA memory lifecycle:
    /// ingest → embed → evaluate → store/index → retrieve.
    /// Integrates working memory, vector index, associations, and (optionally) VectorDB multi-collection routing.
    /// </summary>
    public class MemoryBroker : IMemoryBroker, IRetriever
    {
        // ---------------------------
        // Existing dependencies
        // ---------------------------
        private readonly IMemoryStore _store;
        private readonly IVectorIndex _vector;
        private readonly IEmbeddingProvider _embeddings;
        private readonly IWorkingMemory _working;
        private readonly IAssociationStore? _associations;
        private readonly MemoryBrokerOptions _options;

        // ---------------------------
        // Optional VectorDB dependencies (multi-collection)
        // ---------------------------
        private readonly IVectorDBRouter? _vdbRouter;
        private readonly IVectorDBCollectionManager? _vdbManager;
        private readonly IVectorDBDriver? _vdbDriver;
        private readonly VectorConfig? _vdbConfig;

        public MemoryBroker(
            IEnumerable<IMemoryStore> stores,
            IVectorIndex vector,
            IEmbeddingProvider embeddings,
            IWorkingMemory workingMemory,
            IAssociationStore? associations = null,
            MemoryBrokerOptions? options = null,
            // VectorDB optional deps:
            IVectorDBRouter? vdbRouter = null,
            IVectorDBCollectionManager? vdbManager = null,
            IVectorDBDriver? vdbDriver = null,
            VectorConfig? vdbConfig = null)
        {
            if (stores == null) throw new ArgumentNullException(nameof(stores));
            _store = stores.FirstOrDefault() ?? throw new ArgumentException("At least one store must be provided.", nameof(stores));
            _vector = vector ?? throw new ArgumentNullException(nameof(vector));
            _embeddings = embeddings ?? throw new ArgumentNullException(nameof(embeddings));
            _working = workingMemory ?? throw new ArgumentNullException(nameof(workingMemory));
            _associations = associations;
            _options = options ?? new MemoryBrokerOptions();

            // VectorDB (optional)
            _vdbRouter = vdbRouter;
            _vdbManager = vdbManager;
            _vdbDriver = vdbDriver;
            _vdbConfig = vdbConfig;

            if (_vdbConfig != null)
            {
                // Only validate if VectorDB is intended to be used (i.e., all three pieces present)
                if (IsVectorDBEnabled())
                    _vdbConfig.Validate();
            }
        }

        private bool IsVectorDBEnabled()
            => _vdbRouter != null && _vdbManager != null && _vdbDriver != null && _vdbConfig != null;

        private void VdbLog(string message)
        {
            if (_vdbConfig?.EnableLogging == true)
                Console.WriteLine(message);
        }

        // ---------------------------
        // IMemoryBroker: Records CRUD
        // ---------------------------

        public async Task<string> UpsertAsync(UpsertMemoryRequest request, CancellationToken ct)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(request.Text))
                throw new ArgumentException("Text cannot be null or empty.", nameof(request.Text));

            // Build a base record DTO first (consistent with interface expectations).
            var record = BuildRecordFromRequest(request);

            // Deduplicate metadata keys (case-insensitive) to avoid unique index collisions.
            if (record.Metadata != null && record.Metadata.Count > 0)
            {
                record.Metadata = record.Metadata
                    .GroupBy(m => m.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();
            }

            // Generate vectors now if needed (consistent behavior for decision making).
            float[] embedding = Array.Empty<float>();
            if ((request.Vector != null && request.Vector.Length > 0))
            {
                record.Vectors = request.Vector
                    .Select((v, i) => new MemoryVectorDto { Index = i, Value = v })
                    .ToList();
                embedding = request.Vector.ToArray();
            }
            else if (!string.IsNullOrWhiteSpace(record.Text))
            {
                embedding = await _embeddings.EmbedAsync(record.Text, ct).ConfigureAwait(false);
                record.Vectors = embedding
                    .Select((v, i) => new MemoryVectorDto { Index = i, Value = v })
                    .ToList();
            }

            // Compute novelty & salience
            record.Novelty = await EstimateNoveltyAsync(record, ct).ConfigureAwait(false);
            record.Salience = CalculateSalience(record, record.Novelty);

            // Decide persistence BEFORE any segmentation so we don't create edges for records that won't persist.
            var shouldPersist = ShouldPersist(record, record.Salience);

            // If persisting AND text is too long, segment & link (FK-safe).
            if (shouldPersist && _options.EnableSegmentation && record.Text.Length > _options.MaxSegmentLength)
            {
                return await UpsertSegmentChainAsync(request, ct).ConfigureAwait(false);
            }

            // If NOT persisting, still guard against oversized text going into RAM (redundant safety).
            if (!shouldPersist && record.Text.Length > _options.MaxSegmentLength)
            {
                // Truncate to MaxSegmentLength (no edges in working memory).
                record.Text = record.Text.Substring(0, _options.MaxSegmentLength);
                // Optionally, annotate via metadata (collision-safe).
                record.Metadata ??= new List<MemoryMetadataDto>();
                if (!record.Metadata.Any(m => m.Key.Equals("wm_truncated", StringComparison.OrdinalIgnoreCase)))
                    record.Metadata.Add(new MemoryMetadataDto { Key = "wm_truncated", Value = "true" });
            }

            // Persist vs. working
            if (shouldPersist)
            {
                await _store.UpsertAsync(record, ct).ConfigureAwait(false);
                await _vector.AddAsync(record, ct).ConfigureAwait(false);

                // -------- VectorDB integration (optional) --------
                if (IsVectorDBEnabled())
                {
                    // Create a VectorDBRecord from the DTO
                    var vdbRecord = new VectorDBRecord
                    {
                        Id = record.ID,
                        Vector = embedding,
                        // flatten metadata list -> dictionary (string -> object)
                        Metadata = (record.Metadata ?? new List<MemoryMetadataDto>())
                            .GroupBy(m => m.Key, StringComparer.OrdinalIgnoreCase)
                            .Select(g => g.First())
                            .ToDictionary(m => m.Key, m => (object?)m.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase),
                        Tags = (record.Tags ?? new List<MemoryTagDto>()).Select(t => t.Tag).ToArray()
                    };

                    // If record already uses a "topic" metadata, keep it; otherwise nothing extra here.
                    await UpsertVectorDBAsync(vdbRecord, ct).ConfigureAwait(false);
                }
            }
            else
            {
                var ttl = Math.Max(_options.MinWorkingTtlSeconds, _options.MaxWorkingTtlSeconds * record.Salience);
                await _working.AddOrRefreshAsync(record, TimeSpan.FromSeconds(ttl), ct).ConfigureAwait(false);
            }

            return record.ID;
        }

        public Task<MemoryRecordDto?> GetAsync(string id, bool bumpAccess, CancellationToken ct)
            => _store.GetAsync(id, ct);

        public Task<bool> DeleteAsync(string id, CancellationToken ct)
            => _store.DeleteAsync(id, ct);

        public Task<(IReadOnlyList<MemoryRecordDto> Items, int Total)> ListAsync(int skip, int take, CancellationToken ct)
            => _store.ListAsync(skip, take, ct);

        // ---------------------------
        // IMemoryBroker: Query policy
        // ---------------------------

        public async Task<IReadOnlyList<(MemoryRecordDto Record, float Score)>> SemanticQueryAsync(
            QueryMemoryRequest request, CancellationToken ct)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            // Ensure embedding
            if ((request.Embedding == null || request.Embedding.Length == 0) &&
                !string.IsNullOrWhiteSpace(request.Text))
            {
                request.Embedding = await _embeddings.EmbedAsync(request.Text, ct).ConfigureAwait(false);
            }

            // If VectorDB is enabled, try it first
            if (IsVectorDBEnabled() && request.Embedding != null && request.Embedding.Length > 0)
            {
                // Filter can carry an explicit collection or topic
                var filter = request.Topic ?? request.Collection;
                var vdbResults = await _vdbDriver!.SearchAsync(request.Embedding, request.TopK, filter, ct).ConfigureAwait(false);

                // Map VectorDB hits back to MemoryRecordDto via _store (best effort)
                var mapped = new List<(MemoryRecordDto, float)>(vdbResults.Count);
                foreach (var hit in vdbResults)
                {
                    var dto = await _store.GetAsync(hit.Id, ct).ConfigureAwait(false);
                    if (dto != null)
                    {
                        mapped.Add((dto, hit.Score));
                    }
                    else
                    {
                        // Fallback minimal DTO if store does not have it (should be rare)
                        var fallback = new MemoryRecordDto
                        {
                            ID = hit.Id,
                            Text = hit.Metadata != null && hit.Metadata.TryGetValue("text", out var textObj)
                                ? Convert.ToString(textObj)
                                : string.Empty,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            LastAccessedAt = DateTime.UtcNow,
                            Source = "vdb"
                        };
                        mapped.Add((fallback, hit.Score));
                    }
                }

                return mapped;
            }

            // Fallback to existing vector index path
            var hits = await _vector.QueryAsync(request, ct).ConfigureAwait(false);
            return hits
                .Where(h => h?.Record != null)
                .Select(h => (h.Record, (float)h.Score))
                .ToList();
        }

        // -----------------------------------
        // IMemoryBroker: Association handling
        // -----------------------------------

        public Task<string> UpsertEdgeAsync(AssociationEdgeDto edge, CancellationToken ct)
            => _associations?.UpsertAsync(edge, ct)
                ?? Task.FromResult(edge.ID ?? Guid.NewGuid().ToString("N"));

        public Task<AssociationEdgeDto?> GetEdgeAsync(string id, CancellationToken ct)
            => _associations?.GetAsync(id, ct)
                ?? Task.FromResult<AssociationEdgeDto?>(null);

        public Task<bool> DeleteEdgeAsync(string id, CancellationToken ct)
            => _associations?.DeleteAsync(id, ct)
                ?? Task.FromResult(false);

        public async Task<IReadOnlyList<AssociationEdgeDto>> ListEdgesAsync(int skip, int take, CancellationToken ct)
        {
            if (_associations != null)
            {
                var (items, _) = await _associations.ListAsync(skip, take, ct).ConfigureAwait(false);
                return items;
            }
            return Array.Empty<AssociationEdgeDto>();
        }

        // ----------------------------
        // IMemoryBroker: Working memory
        // ----------------------------

        public Task<IReadOnlyList<MemoryRecordDto>> GetWorkingAsync(CancellationToken ct)
            => _working.GetItemsAsync(ct);

        public Task FlushWorkingAsync(CancellationToken ct)
            => _working.FlushAsync(ct);

        // ----------------------------
        // IRetriever
        // ----------------------------

        public Task<IReadOnlyList<QueryHit>> RetrieveAsync(QueryMemoryRequest request, CancellationToken ct)
            => _vector.QueryAsync(request, ct);

        // ----------------------------
        // Segmentation (FK-safe)
        // ----------------------------

        private async Task<string> UpsertSegmentChainAsync(UpsertMemoryRequest original, CancellationToken ct)
        {
            // Root ID drives segment IDs
            var rootId = string.IsNullOrWhiteSpace(original.Id) ? Guid.NewGuid().ToString("N") : original.Id;
            var text = original.Text ?? string.Empty;
            var segments = SplitText(text, _options.MaxSegmentLength).ToList();
            var segmentIds = new List<string>(capacity: segments.Count);

            // 1) Insert all segments (records only)
            for (int i = 0; i < segments.Count; i++)
            {
                var segId = $"{rootId}:seg-{(i + 1):D3}";
                segmentIds.Add(segId);

                var segReq = new UpsertMemoryRequest
                {
                    Id = segId,
                    Text = segments[i],
                    EpisodeId = original.EpisodeId,
                    ContextId = original.ContextId,
                    Tags = (original.Tags ?? Array.Empty<string>())
                        .Concat(new[] { _options.SegmentTag })
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToArray(),
                    Metadata = original.Metadata,
                    Salience = original.Salience,
                    Novelty = original.Novelty,
                    Frequency = original.Frequency,
                    DecayRate = original.DecayRate
                };

                // Use the same pipeline for each segment (this will persist since we decided to segment only when persisting)
                await UpsertSinglePersistingAsync(segReq, ct).ConfigureAwait(false);
            }

            // 2) Link with edges after all records exist (FK-safe)
            if (_associations != null && segmentIds.Count > 1)
            {
                for (int i = 1; i < segmentIds.Count; i++)
                {
                    var from = segmentIds[i - 1];
                    var to = segmentIds[i];

                    var edge = new AssociationEdgeDto
                    {
                        ID = Guid.NewGuid().ToString("N"),
                        FromID = from,
                        ToID = to,
                        Type = _options.SegmentEdgeType,
                        Weight = 1.0,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _associations.UpsertAsync(edge, ct).ConfigureAwait(false);
                }
            }

            return segmentIds.First();
        }

        /// <summary>
        /// Persisting path for one (already sized) segment. Always persists + indexes (+ VectorDB if enabled).
        /// </summary>
        private async Task<string> UpsertSinglePersistingAsync(UpsertMemoryRequest request, CancellationToken ct)
        {
            var record = BuildRecordFromRequest(request);

            // Deduplicate metadata
            if (record.Metadata != null && record.Metadata.Count > 0)
            {
                record.Metadata = record.Metadata
                    .GroupBy(m => m.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .ToList();
            }

            // Embed per segment
            float[] embedding = Array.Empty<float>();
            if (!string.IsNullOrWhiteSpace(record.Text))
            {
                embedding = await _embeddings.EmbedAsync(record.Text, ct).ConfigureAwait(false);
                record.Vectors = embedding.Select((v, i) => new MemoryVectorDto { Index = i, Value = v }).ToList();
            }

            // Compute novelty/salience for completeness (not required for FK)
            record.Novelty = await EstimateNoveltyAsync(record, ct).ConfigureAwait(false);
            record.Salience = CalculateSalience(record, record.Novelty);

            await _store.UpsertAsync(record, ct).ConfigureAwait(false);
            await _vector.AddAsync(record, ct).ConfigureAwait(false);

            // VectorDB path (optional)
            if (IsVectorDBEnabled())
            {
                var vdbRecord = new VectorDBRecord
                {
                    Id = record.ID,
                    Vector = embedding,
                    Metadata = (record.Metadata ?? new List<MemoryMetadataDto>())
                        .GroupBy(m => m.Key, StringComparer.OrdinalIgnoreCase)
                        .Select(g => g.First())
                        .ToDictionary(m => m.Key, m => (object?)m.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase),
                    Tags = (record.Tags ?? new List<MemoryTagDto>()).Select(t => t.Tag).ToArray()
                };

                await UpsertVectorDBAsync(vdbRecord, ct).ConfigureAwait(false);
            }

            return record.ID;
        }

        private async Task UpsertVectorDBAsync(VectorDBRecord vdbRecord, CancellationToken ct)
        {
            if (!IsVectorDBEnabled()) return;

            // Resolve collection via router and ensure it exists
            var collectionName = _vdbRouter!.GetTargetCollection(vdbRecord);
            var collection = new VectorDBCollectionDto
            {
                Name = collectionName,
                Dimension = vdbRecord.Vector?.Length ?? _vdbConfig!.Dimension,
                DistanceMetric = _vdbConfig!.Metric,
                Metric = _vdbConfig!.Metric,
                IsInitialized = true
            };

            await _vdbManager!.CreateIfNotExistsAsync(collection, ct).ConfigureAwait(false);
            await _vdbDriver!.UpsertAsync(vdbRecord, ct).ConfigureAwait(false);

            VdbLog($"[VectorDB] Upserted '{vdbRecord.Id}' into '{collectionName}'.");
        }

        private static IEnumerable<string> SplitText(string input, int maxLen)
        {
            if (string.IsNullOrEmpty(input)) yield break;
            for (int i = 0; i < input.Length; i += maxLen)
                yield return input.Substring(i, Math.Min(maxLen, input.Length - i));
        }

        private static MemoryRecordDto BuildRecordFromRequest(UpsertMemoryRequest request)
        {
            var r = new MemoryRecordDto
            {
                ID = string.IsNullOrWhiteSpace(request.Id) ? Guid.NewGuid().ToString() : request.Id,
                Text = request.Text ?? string.Empty,
                EpisodeId = request.EpisodeId,
                ContextId = request.ContextId,
                Salience = request.Salience,
                Novelty = request.Novelty,
                Frequency = request.Frequency,
                DecayRate = request.DecayRate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                Source = "broker"
            };

            if (request.Tags != null)
                r.Tags = new List<MemoryTagDto>(request.Tags.Select(t => new MemoryTagDto { Tag = t }));

            if (request.Metadata != null)
                r.Metadata = new List<MemoryMetadataDto>(
                    request.Metadata.Select(kv => new MemoryMetadataDto { Key = kv.Key, Value = kv.Value?.ToString() })
                );

            return r;
        }

        // ----------------------------
        // Scoring & policy helpers
        // ----------------------------

        private async Task<double> EstimateNoveltyAsync(MemoryRecordDto record, CancellationToken ct)
        {
            if (record.Vectors == null || record.Vectors.Count == 0)
                return 1.0;

            try
            {
                var embedding = record.Vectors
                    .OrderBy(v => v.Index)
                    .Select(v => (float)v.Value)
                    .ToArray();

                var req = new QueryMemoryRequest
                {
                    Embedding = embedding,
                    MinScore = 0.0f,
                    TopK = 1
                };

                var neighbors = await _vector.QueryAsync(req, ct).ConfigureAwait(false);
                var best = neighbors.FirstOrDefault();
                if (best == null) return 1.0;

                var score = Clamp(best.Score, 0.0, 1.0);
                return 1.0 - score;
            }
            catch
            {
                return 1.0;
            }
        }

        private double CalculateSalience(MemoryRecordDto r, double novelty)
        {
            var now = DateTime.UtcNow;
            var created = r.CreatedAt == default ? now : r.CreatedAt;
            var ageSeconds = Math.Max(0, (now - created).TotalSeconds);

            var halfLife = Math.Max(1.0, _options.RecencyHalfLifeSeconds);
            var recency = Math.Exp(-ageSeconds * Math.Log(2) / halfLife);
            var freq = Clamp(r.Frequency, 0.0, 1.0);

            var weighted =
                _options.WeightNovelty * novelty +
                _options.WeightRecency * recency +
                _options.WeightFrequency * freq;

            var denom = Math.Max(1e-6, _options.WeightNovelty + _options.WeightRecency + _options.WeightFrequency);
            return Clamp(weighted / denom, 0.0, 1.0);
        }

        private bool ShouldPersist(MemoryRecordDto r, double salience)
        {
            var tags = r.Tags?.Select(t => t.Tag).ToArray() ?? Array.Empty<string>();
            var tagSet = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);

            if (tagSet.Any(t => _options.AlwaysPersistTags.Contains(t))) return true;
            if (tagSet.Any(t => _options.NeverPersistTags.Contains(t))) return false;

            return salience >= _options.PersistThreshold;
        }

        private static double Clamp(double value, double min, double max)
            => value < min ? min : value > max ? max : value;
    }

    /// <summary>
    /// Configurable runtime parameters for MemoryBroker behavior.
    /// </summary>
    public class MemoryBrokerOptions
    {
        public double DefaultMinScore { get; set; } = 0.0;
        public int MaxRetrieve { get; set; } = 32;

        public double PersistThreshold { get; set; } = 0.55;
        public double WeightNovelty { get; set; } = 0.5;
        public double WeightRecency { get; set; } = 0.3;
        public double WeightFrequency { get; set; } = 0.2;
        public double RecencyHalfLifeSeconds { get; set; } = 3600;

        public double MinWorkingTtlSeconds { get; set; } = 30;
        public double MaxWorkingTtlSeconds { get; set; } = 300;

        public bool ReinforceTopHits { get; set; } = true;
        public int ReinforceTopN { get; set; } = 5;
        public double ReinforceTtlSeconds { get; set; } = 120;

        public string[] AlwaysPersistTags { get; set; } = new[] { "system", "core", "identity" };
        public string[] NeverPersistTags { get; set; } = new[] { "ephemeral", "scratch" };

        // Segmentation options
        public bool EnableSegmentation { get; set; } = true;
        public int MaxSegmentLength { get; set; } = 4000; // keeps behavior consistent even with NVARCHAR(MAX)
        public string SegmentEdgeType { get; set; } = "same_record";
        public string SegmentTag { get; set; } = "segmented";

    }
}
