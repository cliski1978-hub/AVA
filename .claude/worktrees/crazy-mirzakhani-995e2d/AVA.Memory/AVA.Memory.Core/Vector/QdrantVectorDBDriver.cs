using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;

namespace AVA.Memory.Core.Vector
{
    /// <summary>
    /// Qdrant implementation of <see cref="IVectorDBDriver"/> using the REST API.
    /// Supports dynamic multi-collection management and point CRUD/search operations.
    /// </summary>
    public sealed class QdrantVectorDBDriver : IVectorDBDriver, IDisposable
    {
        #region Private Fields

        private readonly HttpClient _http;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly string _baseUrl;
        private bool _disposed;

        #endregion

        #region Constructors

        public QdrantVectorDBDriver(string endpoint, string apiKey = null)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
                throw new ArgumentException("Qdrant endpoint must be provided.", nameof(endpoint));

            _baseUrl = endpoint.TrimEnd('/');
            _http = new HttpClient();

            if (!string.IsNullOrWhiteSpace(apiKey))
                _http.DefaultRequestHeaders.Add("api-key", apiKey);

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        #endregion

        #region Collection Management

        public async Task<bool> EnsureCollectionAsync(VectorDBCollectionDto collection, CancellationToken ct = default)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            if (string.IsNullOrWhiteSpace(collection.Name))
                throw new ArgumentException("Collection name cannot be null or empty.", nameof(collection));

            // Check if collection already exists
            var resp = await _http.GetAsync($"{_baseUrl}/collections/{collection.Name}", ct);
            if (resp.IsSuccessStatusCode)
            {
                collection.IsInitialized = true;
                return true;
            }

            // Create collection
            var payload = new
            {
                vectors = new
                {
                    size = collection.Dimension,
                    distance = collection.DistanceMetric ?? "Cosine"
                }
            };

            var createResp = await _http.PutAsJsonAsync(
                $"{_baseUrl}/collections/{collection.Name}",
                payload,
                _jsonOptions,
                ct);

            collection.IsInitialized = createResp.IsSuccessStatusCode;
            return collection.IsInitialized;
        }

        public async Task<IReadOnlyList<VectorDBCollectionDto>> ListCollectionsAsync(CancellationToken ct = default)
        {
            var resp = await _http.GetAsync($"{_baseUrl}/collections", ct);
            if (!resp.IsSuccessStatusCode)
                return Array.Empty<VectorDBCollectionDto>();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var list = new List<VectorDBCollectionDto>();

            if (doc.RootElement.TryGetProperty("result", out var result))
            {
                foreach (var item in result.EnumerateArray())
                {
                    if (item.TryGetProperty("name", out var nameProp))
                    {
                        list.Add(new VectorDBCollectionDto
                        {
                            Name = nameProp.GetString() ?? string.Empty,
                            IsInitialized = true
                        });
                    }
                }
            }

            return list;
        }

        public async Task<bool> DeleteCollectionAsync(string name, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Collection name cannot be null or empty.", nameof(name));

            var resp = await _http.DeleteAsync($"{_baseUrl}/collections/{name}", ct);
            return resp.IsSuccessStatusCode;
        }

        #endregion

        #region CRUD Operations

        public async Task UpsertAsync(VectorDBRecord record, CancellationToken ct = default)
        {
            if (record == null || record.Vector == null || record.Vector.Length == 0)
                throw new ArgumentException("Record must contain a valid vector.", nameof(record));

            var collection = ResolveCollectionName(record);
            var payload = new
            {
                points = new[]
                {
                    new
                    {
                        id = record.Id ?? Guid.NewGuid().ToString("N"),
                        vector = record.Vector,
                        payload = record.Metadata
                    }
                }
            };

            var response = await _http.PutAsJsonAsync(
                $"{_baseUrl}/collections/{collection}/points?wait=true",
                payload,
                _jsonOptions,
                ct);

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Qdrant upsert failed for '{collection}': {err}");
            }
        }

        public async Task DeleteAsync(string id, string? collection = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Invalid ID for delete operation.", nameof(id));

            var targetCollection = !string.IsNullOrWhiteSpace(collection)
                ? collection
                : "ava_memory";

            var payload = new { points = new[] { id } };

            var resp = await _http.PostAsJsonAsync(
                $"{_baseUrl}/collections/{targetCollection}/points/delete",
                payload,
                _jsonOptions,
                ct);

            resp.EnsureSuccessStatusCode();
        }

        #endregion

        #region Search

        public async Task<IReadOnlyList<VectorDbSearchResult>> SearchAsync(
            float[] vector,
            int topK,
            string? filter = null,
            CancellationToken ct = default)
        {
            if (vector == null || vector.Length == 0)
                throw new ArgumentException("Search vector cannot be null or empty.", nameof(vector));

            var collection = string.IsNullOrWhiteSpace(filter) ? "ava_memory" : filter.Trim();

            var payload = new
            {
                vector,
                limit = topK,
                with_payload = true
            };

            var resp = await _http.PostAsJsonAsync(
                $"{_baseUrl}/collections/{collection}/points/search",
                payload,
                _jsonOptions,
                ct);

            if (!resp.IsSuccessStatusCode)
                throw new InvalidOperationException($"Search failed in '{collection}': {resp.ReasonPhrase}");

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var results = new List<VectorDbSearchResult>();

            if (doc.RootElement.TryGetProperty("result", out var result))
            {
                foreach (var r in result.EnumerateArray())
                {
                    var id = r.GetProperty("id").ToString();
                    var score = r.GetProperty("score").GetSingle();

                    Dictionary<string, object>? payloadDict = null;
                    if (r.TryGetProperty("payload", out var payloadElem))
                    {
                        payloadDict = payloadElem.EnumerateObject()
                            .ToDictionary(p => p.Name, p => (object)p.Value.ToString());
                    }

                    results.Add(new VectorDbSearchResult(id, score, payloadDict));
                }
            }

            return results;
        }

        #endregion

        #region Diagnostics

        /// <summary>
        /// Retrieves total vector count for a collection using Qdrant REST API.
        /// </summary>
        public async Task<int> GetVectorCountAsync(string collection, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException(nameof(collection));

            var resp = await _http.GetAsync($"{_baseUrl}/collections/{collection}", ct);
            if (!resp.IsSuccessStatusCode)
                return 0;

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("points_count", out var countProp) &&
                countProp.TryGetInt32(out var count))
            {
                return count;
            }

            return 0;
        }

        /// <summary>
        /// Samples a small set of vectors from the collection for inspection.
        /// </summary>
        public async Task<IReadOnlyList<VectorDBRecord>> SampleVectorsAsync(
            string collection,
            int sampleCount,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(collection))
                throw new ArgumentNullException(nameof(collection));

            // Qdrant 1.8+ supports /points/scroll for iterating through stored vectors
            var payload = new
            {
                limit = sampleCount,
                with_payload = true,
                with_vector = true
            };

            var resp = await _http.PostAsJsonAsync(
                $"{_baseUrl}/collections/{collection}/points/scroll",
                payload,
                _jsonOptions,
                ct);

            if (!resp.IsSuccessStatusCode)
                return Array.Empty<VectorDBRecord>();

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
            var results = new List<VectorDBRecord>();

            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("points", out var pointsElem))
            {
                foreach (var p in pointsElem.EnumerateArray())
                {
                    var id = p.GetProperty("id").ToString();
                    var vectorArray = p.GetProperty("vector")
                        .EnumerateArray()
                        .Select(v => v.GetSingle())
                        .ToArray();

                    Dictionary<string, object>? metadata = null;
                    if (p.TryGetProperty("payload", out var payloadElem))
                    {
                        metadata = payloadElem.EnumerateObject()
                            .ToDictionary(e => e.Name, e => (object)e.Value.ToString());
                    }

                    results.Add(new VectorDBRecord
                    {
                        Id = id,
                        Vector = vectorArray,
                        Metadata = metadata ?? new Dictionary<string, object>(),
                        Collection = collection
                    });
                }
            }

            return results;
        }

        #endregion

        #region Helpers

        private static string ResolveCollectionName(VectorDBRecord record)
        {
            if (record.Metadata != null && record.Metadata.TryGetValue("topic", out var topic))
                return NormalizeName(topic?.ToString());

            if (record.Tags != null && record.Tags.Length > 0)
                return NormalizeName(record.Tags[0]);

            return "ava_memory";
        }

        private static string NormalizeName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "ava_memory";

            var safe = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-').ToArray());
            return safe.Length > 0 ? safe.ToLowerInvariant() : "ava_memory";
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;
            _http.Dispose();
            _disposed = true;
        }

        #endregion
    }
}
