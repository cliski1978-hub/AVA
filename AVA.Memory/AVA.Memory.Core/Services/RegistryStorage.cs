using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;

namespace AVA.Memory.Core.Services
{
    /// <summary>
    /// Provides an in-memory implementation of IVectorDBCollectionRegistry.
    /// Maintains a thread-safe registry of known collections and synchronizes
    /// with backend VectorDB state when requested.
    /// </summary>
    public sealed class RegistryStorage : IVectorDBCollectionRegistry
    {
        #region Private Fields

        private readonly ConcurrentDictionary<string, VectorDBCollectionDto> _collections;

        #endregion

        #region Constructor

        public RegistryStorage()
        {
            _collections = new ConcurrentDictionary<string, VectorDBCollectionDto>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region Core Registry Operations

        /// <summary>
        /// Registers a new collection or updates an existing entry in the registry.
        /// </summary>
        public Task RegisterOrUpdateAsync(VectorDBCollectionDto collection, CancellationToken ct)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            _collections[collection.Name] = collection;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Removes a collection entry from the registry.
        /// </summary>
        public Task<bool> RemoveAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Task.FromResult(false);

            return Task.FromResult(_collections.TryRemove(name, out _));
        }

        /// <summary>
        /// Returns all collection entries currently registered.
        /// </summary>
        public Task<IReadOnlyList<VectorDBCollectionDto>> ListAsync(CancellationToken ct)
        {
            var list = _collections.Values.ToList();
            return Task.FromResult<IReadOnlyList<VectorDBCollectionDto>>(list);
        }

        /// <summary>
        /// Retrieves a collection by name.
        /// </summary>
        public Task<VectorDBCollectionDto?> GetAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Task.FromResult<VectorDBCollectionDto?>(null);

            _collections.TryGetValue(name, out var result);
            return Task.FromResult(result);
        }

        /// <summary>
        /// Checks whether a collection is already registered.
        /// </summary>
        public Task<bool> ExistsAsync(string name, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Task.FromResult(false);

            return Task.FromResult(_collections.ContainsKey(name));
        }

        /// <summary>
        /// Clears the entire in-memory registry.
        /// </summary>
        public Task ClearAsync(CancellationToken ct)
        {
            _collections.Clear();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Synchronizes the registry with the backend.
        /// For now this is a stub; it can be expanded to pull from the active driver.
        /// </summary>
        public Task<bool> SyncWithBackendAsync(CancellationToken ct)
        {
            Console.WriteLine("[VectorDB] RegistryStorage.SyncWithBackendAsync called (stub implementation).");
            return Task.FromResult(true);
        }

        #endregion
    }
}
