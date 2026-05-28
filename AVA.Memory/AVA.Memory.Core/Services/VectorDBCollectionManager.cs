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
    /// Manages VectorDB collection lifecycle operations across
    /// the backend driver and optional registry.
    /// </summary>
    public sealed class VectorDBCollectionManager : IVectorDBCollectionManager
    {
        #region Private Fields

        private readonly IVectorDBDriver _driver;
        private readonly VectorConfig _config;

        #endregion

        #region Constructor

        public VectorDBCollectionManager(IVectorDBDriver driver, VectorConfig config)
        {
            _driver = driver ?? throw new ArgumentNullException(nameof(driver));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        #endregion

        #region Creation and Existence

        public async Task<bool> CreateIfNotExistsAsync(VectorDBCollectionDto collection, CancellationToken ct)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            var exists = await ExistsAsync(collection.Name, ct);
            if (exists)
                return true;

            var newCollection = new VectorDBCollectionDto
            {
                Name = collection.Name,
                Dimension = collection.Dimension > 0 ? collection.Dimension : _config.Dimension,
                Metric = collection.Metric ?? _config.Metric,
                CreatedAt = DateTime.UtcNow,
                IsInitialized = true
            };

            return await _driver.EnsureCollectionAsync(newCollection, ct);
        }

        public async Task<bool> ExistsAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var all = await _driver.ListCollectionsAsync(ct);
            return all.Any(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Deletion

        /// <summary>
        /// Deletes a collection via the backend driver.
        /// </summary>
        public async Task<bool> DeleteAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Collection name must be provided.", nameof(name));

            Console.WriteLine($"[VectorDB] Deleting collection '{name}' via driver...");

            var deleted = await _driver.DeleteCollectionAsync(name, ct);

            if (deleted)
                Console.WriteLine($"[VectorDB] Collection '{name}' deleted successfully.");
            else
                Console.WriteLine($"[VectorDB] Collection '{name}' deletion failed or collection not found.");

            return deleted;
        }

        #endregion

        #region Synchronization

        public async Task<bool> SyncAsync(CancellationToken ct)
        {
            var backendCollections = await _driver.ListCollectionsAsync(ct);
            Console.WriteLine($"[VectorDB] Synced {backendCollections.Count} collections from backend.");
            return backendCollections.Count > 0;
        }

        #endregion

        #region Listing

        /// <summary>
        /// Lists all collections known to the backend VectorDB.
        /// </summary>
        public async Task<IReadOnlyList<VectorDBCollectionDto>> ListCollectionsAsync(CancellationToken ct)
        {
            var collections = await _driver.ListCollectionsAsync(ct);

            // Ensure DTOs are returned with full metadata
            return collections.Select(c => new VectorDBCollectionDto
            {
                Name = c.Name,
                Dimension = c.Dimension,
                Metric = c.Metric,
                IsInitialized = c.IsInitialized,
                VectorCount = c.VectorCount,
                CreatedAt = c.CreatedAt,
                LastUpdated = c.LastUpdated
            }).ToList();
        }

        #endregion
    }
}
