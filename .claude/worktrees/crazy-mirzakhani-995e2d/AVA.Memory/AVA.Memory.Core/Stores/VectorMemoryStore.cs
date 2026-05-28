using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;
using AVA.Memory.Core.Configuration;

namespace AVA.Memory.Core.Stores
{
    /// <summary>
    /// Provides a unified interface for managing <see cref="MemoryRecordDto"/> persistence
    /// within the VectorDB backend, using abstraction-layer DTOs only.
    /// </summary>
    public sealed class VectorMemoryStore
    {
        #region Private Fields

        private readonly IVectorDBDriver _driver;
        private readonly IVectorDBCollectionManager _collections;
        private readonly VectorConfig _config;

        #endregion

        #region Constructor

        public VectorMemoryStore(
            IVectorDBDriver driver,
            IVectorDBCollectionManager collections,
            VectorConfig config)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _collections = collections ?? throw new ArgumentNullException(nameof(collections));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region Upsert

        /// <summary>
        /// Inserts or updates a memory record in the VectorDB backend.
        /// Builds the embedding array from the Values of <see cref="MemoryVectorDto"/>.
        /// </summary>
        public async Task UpsertAsync(MemoryRecordDto record, CancellationToken ct = default)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            if (record.Vectors == null || record.Vectors.Count == 0)
                throw new ArgumentException("MemoryRecordDto must include at least one vector component.", nameof(record));

            // Build the embedding array from ordered components
            var primaryVector = record.Vectors
                .OrderBy(v => v.Index)
                .Select(v => v.Value)
                .ToArray();

            if (primaryVector.Length == 0)
                throw new ArgumentException("Primary vector cannot be empty.", nameof(record));

            // ✅ Determine which collection to use
            var targetCollection = ResolveCollection(record);

            await _collections.CreateIfNotExistsAsync(new VectorDBCollectionDto
            {
                Name = targetCollection,
                Dimension = _config.Dimension,
                Metric = _config.Metric
            }, ct);

            var metadata = ToMetadataDictionary(record);

            var vectorRecord = new VectorDBRecord
            {
                Id = record.ID ?? Guid.NewGuid().ToString("N"),
                Collection = targetCollection,   // ✅ This line fixes the issue
                Vector = primaryVector,
                Metadata = metadata
            };

            await _driver.UpsertAsync(vectorRecord, ct);
        }


        #endregion

        #region Search

        /// <summary>
        /// Performs a similarity search and maps results to <see cref="MemoryRecordDto"/>.
        /// </summary>
        public async Task<IReadOnlyList<MemoryRecordDto>> SearchAsync(
            float[] vector,
            int topK = 10,
            string? collection = null,
            CancellationToken ct = default)
        {
            if (vector == null || vector.Length == 0)
                throw new ArgumentException("Search vector cannot be null or empty.", nameof(vector));

            var targetCollection = !string.IsNullOrWhiteSpace(collection)
                ? collection
                : _config.DefaultCollection;

            var results = await _driver.SearchAsync(vector, topK, targetCollection, ct);
            return results.Select(ToMemoryRecordDto).ToList();
        }

        #endregion

        #region Delete

        public async Task<bool> DeleteAsync(string id, string? collection = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Memory ID must be provided.", nameof(id));

            var targetCollection = !string.IsNullOrWhiteSpace(collection)
                ? collection
                : _config.DefaultCollection;

            await _driver.DeleteAsync(id, targetCollection, ct);
            return true;
        }

        #endregion

        #region Helpers

        private static string ResolveCollection(MemoryRecordDto record)
        {
            if (record.Tags != null && record.Tags.Count > 0)
                return NormalizeName(record.Tags[0].Tag);

            if (!string.IsNullOrWhiteSpace(record.ContextId))
                return NormalizeName(record.ContextId);

            return "ava_memory";
        }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "ava_memory";

            var safe = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            return safe.Length > 0 ? safe.ToLowerInvariant() : "ava_memory";
        }

        private static Dictionary<string, object> ToMetadataDictionary(MemoryRecordDto record)
        {
            var meta = new Dictionary<string, object>();

            if (record.Metadata != null)
            {
                foreach (var m in record.Metadata)
                    meta[m.Key] = m.Value ?? string.Empty;
            }

            meta["text"] = record.Text ?? string.Empty;
            meta["source"] = record.Source ?? "unknown";
            meta["createdAt"] = record.CreatedAt.ToString("o");
            meta["updatedAt"] = record.UpdatedAt.ToString("o");
            meta["salience"] = record.Salience;
            meta["novelty"] = record.Novelty;
            meta["frequency"] = record.Frequency;
            meta["decayRate"] = record.DecayRate;
            meta["tags"] = record.Tags != null
                ? string.Join(",", record.Tags.Select(t => t.Tag))
                : string.Empty;

            return meta;
        }

        private static MemoryRecordDto ToMemoryRecordDto(VectorDbSearchResult result)
        {
            var dto = new MemoryRecordDto
            {
                ID = result.Id,
                Text = result.Metadata != null && result.Metadata.ContainsKey("text")
                    ? result.Metadata["text"]?.ToString()
                    : null,
                Metadata = result.Metadata?.Select(kv => new MemoryMetadataDto
                {
                    Key = kv.Key,
                    Value = kv.Value?.ToString()
                }).ToList() ?? new List<MemoryMetadataDto>(),
                CreatedAt = ParseDate(result.Metadata, "createdAt"),
                UpdatedAt = ParseDate(result.Metadata, "updatedAt"),
                Source = result.Metadata != null && result.Metadata.ContainsKey("source")
                    ? result.Metadata["source"]?.ToString()
                    : null
            };

            // Map tags
            if (result.Metadata != null && result.Metadata.ContainsKey("tags"))
            {
                var raw = result.Metadata["tags"]?.ToString() ?? string.Empty;
                var tags = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                dto.Tags = tags.Select(t => new MemoryTagDto { Tag = t.Trim() }).ToList();
            }

            // Map vector back to MemoryVectorDto list
            if (result.Vector != null && result.Vector.Length > 0)
            {
                dto.Vectors = result.Vector
                    .Select((v, i) => new MemoryVectorDto
                    {
                        Index = i,
                        Value = v,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    })
                    .ToList();
            }

            return dto;
        }

        private static DateTime ParseDate(Dictionary<string, object>? dict, string key)
        {
            if (dict != null && dict.TryGetValue(key, out var val))
            {
                if (DateTime.TryParse(val?.ToString(), out var parsed))
                    return parsed;
            }
            return DateTime.UtcNow;
        }

        #endregion
    }
}
