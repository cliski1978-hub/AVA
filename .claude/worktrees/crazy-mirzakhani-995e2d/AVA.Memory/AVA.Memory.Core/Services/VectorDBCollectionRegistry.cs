using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;
using AVA.Memory.Core.Configuration;

namespace AVA.Memory.Core.Services
{
    /// <summary>
    /// Implements persistent storage for VectorDB collection metadata.
    /// Tracks all known collections, their statistics, and update timestamps.
    /// Persists data to a JSON file (e.g., Data/collections.json) defined in VectorConfig.
    /// </summary>
    public sealed class VectorDBCollectionRegistry : IVectorDBCollectionRegistry
    {
        #region Fields

        private readonly string _registryPath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private List<VectorDBCollectionDto> _collections = new();

        #endregion

        #region Constructors

        public VectorDBCollectionRegistry(VectorConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            config.Validate();

            _registryPath = config.RegistryPath ??
                            Path.Combine(AppContext.BaseDirectory, "Data", "collections.json");

            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };

            LoadFromDiskAsync(CancellationToken.None).GetAwaiter().GetResult();
        }

        #endregion

        #region Public Methods

        /// <inheritdoc />
        public async Task<IReadOnlyList<VectorDBCollectionDto>> ListAsync(CancellationToken ct)
        {
            await _lock.WaitAsync(ct);
            try
            {
                return _collections.ToList();
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<VectorDBCollectionDto?> GetAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Collection name cannot be null or empty.", nameof(name));

            await _lock.WaitAsync(ct);
            try
            {
                return _collections.FirstOrDefault(c =>
                    string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc />
        public async Task RegisterOrUpdateAsync(VectorDBCollectionDto collection, CancellationToken ct)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            await _lock.WaitAsync(ct);
            try
            {
                var existing = _collections.FirstOrDefault(c =>
                    string.Equals(c.Name, collection.Name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    existing.VectorCount = collection.VectorCount;
                    existing.Dimension = collection.Dimension;
                    existing.Metric = collection.Metric;
                    existing.LastUpdated = DateTime.UtcNow;
                    existing.Centroid = collection.Centroid;
                    existing.Metadata = collection.Metadata;
                }
                else
                {
                    collection.CreatedAt = DateTime.UtcNow;
                    collection.LastUpdated = DateTime.UtcNow;
                    _collections.Add(collection);
                }

                await SaveToDiskAsync(ct);
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> RemoveAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Collection name cannot be null or empty.", nameof(name));

            await _lock.WaitAsync(ct);
            try
            {
                var removed = _collections.RemoveAll(c =>
                    string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)) > 0;

                if (removed)
                    await SaveToDiskAsync(ct);

                return removed;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <inheritdoc />
        public async Task<bool> SyncWithBackendAsync(CancellationToken ct)
        {
            // This method will be implemented later in VectorDBCollectionManager
            // to reconcile backend collections with the local registry.
            await Task.CompletedTask;
            return true;
        }

        #endregion

        #region Private Helpers

        private async Task LoadFromDiskAsync(CancellationToken ct)
        {
            try
            {
                if (!File.Exists(_registryPath))
                {
                    var dir = Path.GetDirectoryName(_registryPath);
                    if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    await SaveToDiskAsync(ct); // create empty file
                    return;
                }

                using var stream = File.OpenRead(_registryPath);
                var data = await JsonSerializer.DeserializeAsync<List<VectorDBCollectionDto>>(stream, _jsonOptions, ct);
                _collections = data ?? new List<VectorDBCollectionDto>();
            }
            catch
            {
                _collections = new List<VectorDBCollectionDto>();
            }
        }

        private async Task SaveToDiskAsync(CancellationToken ct)
        {
            using var stream = File.Create(_registryPath);
            await JsonSerializer.SerializeAsync(stream, _collections, _jsonOptions, ct);
        }

        #endregion
    }
}
