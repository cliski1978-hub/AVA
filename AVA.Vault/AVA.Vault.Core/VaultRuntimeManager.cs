using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Registry;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core
{
    /// <summary>
    /// Orchestrates Vault instances at runtime.
    /// Allows AVA or host applications to spawn, retrieve, and dispose Vault contexts.
    /// </summary>
    public sealed class VaultRuntimeManager
    {
        private readonly IServiceProvider _rootProvider;
        private readonly VaultRegistry _registry;
        private readonly ConcurrentDictionary<string, IServiceScope> _scopes = new();

        public VaultRuntimeManager(IServiceProvider rootProvider, VaultRegistry registry)
        {
            _rootProvider = rootProvider;
            _registry = registry;
        }

        // -------------------------------------------------------------
        // Create / Start a new Vault context
        // -------------------------------------------------------------

        public async Task<IVaultDbContext> CreateVaultAsync(VaultInstanceConfig config, CancellationToken ct = default)
        {
            // Build a VaultInstance based on configuration
            var instance = new VaultInstance
            {
                VaultID = config.VaultID,
                DisplayName = config.DisplayName,
                VaultPath = config.StoragePath,
                Config = config,
                IsActive = true,
                LastSyncedAt = DateTime.UtcNow
            };

            // Register the vault instance in the registry
            await _registry.RegisterVaultAsync(instance, ct);

            // Create a scoped service container for this vault
            var scope = _rootProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IVaultDbContext>();

            await db.FlushAsync(ct); // Initialize or clear any pending state
            _scopes[instance.VaultID] = scope;

            return db;
        }

        // -------------------------------------------------------------
        // Retrieve an existing Vault context
        // -------------------------------------------------------------

        public IVaultDbContext GetVault(string vaultId)
        {
            if (!_scopes.TryGetValue(vaultId, out var scope))
                throw new KeyNotFoundException($"Vault {vaultId} not found in runtime registry.");

            return scope.ServiceProvider.GetRequiredService<IVaultDbContext>();
        }

        // -------------------------------------------------------------
        // Dispose / Stop a Vault context
        // -------------------------------------------------------------

        public async Task DisposeVaultAsync(string vaultId, CancellationToken ct = default)
        {
            if (_scopes.TryRemove(vaultId, out var scope))
            {
                scope.Dispose();
                await _registry.UnregisterVaultAsync(vaultId, ct);
            }
        }
    }
}
