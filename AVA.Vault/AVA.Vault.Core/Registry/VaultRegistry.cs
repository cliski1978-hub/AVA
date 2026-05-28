using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Interfaces;

namespace AVA.Vault.Core.Registry
{
    /// <summary>
    /// Central runtime registry for all known Vault instances.
    /// Supports registration, lookup, and persistence to disk.
    /// </summary>
    public sealed class VaultRegistry : IVaultRegistry
    {
        private readonly ConcurrentDictionary<string, VaultInstance> _vaults = new();
        private readonly string _registryPath;

        public VaultRegistry(string? registryPath = null)
        {
            _registryPath = registryPath ??
                            Path.Combine(AppContext.BaseDirectory, "vault_registry.json");
        }

        // -------------------------------------------------------------
        // Registration
        // -------------------------------------------------------------

        public async Task RegisterVaultAsync(VaultInstance vault, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(vault.VaultID))
                throw new InvalidOperationException("VaultID must be assigned by Identity before registration.");

            _vaults[vault.VaultID] = vault;
            await SaveAsync(ct).ConfigureAwait(false);
        }

        public async Task UnregisterVaultAsync(string vaultId, CancellationToken ct = default)
        {
            _vaults.TryRemove(vaultId, out _);
            await SaveAsync(ct).ConfigureAwait(false);
        }

        // -------------------------------------------------------------
        // Retrieval
        // -------------------------------------------------------------

        public Task<VaultInstance?> GetVaultAsync(string vaultId, CancellationToken ct = default)
        {
            _vaults.TryGetValue(vaultId, out var vault);
            return Task.FromResult(vault);
        }

        public Task<IReadOnlyCollection<VaultInstance>> GetAllVaultsAsync(CancellationToken ct = default)
        {
            return Task.FromResult((IReadOnlyCollection<VaultInstance>)_vaults.Values.ToList());
        }


        // -------------------------------------------------------------
        // Persistence
        // -------------------------------------------------------------

        public async Task SaveAsync(CancellationToken ct = default)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var json = JsonSerializer.Serialize(_vaults.Values, options);
            await File.WriteAllTextAsync(_registryPath, json, ct).ConfigureAwait(false);
        }

        public async Task LoadAsync(CancellationToken ct = default)
        {
            if (!File.Exists(_registryPath))
                return;

            var json = await File.ReadAllTextAsync(_registryPath, ct).ConfigureAwait(false);
            var items = JsonSerializer.Deserialize<List<VaultInstance>>(json) ?? new List<VaultInstance>();

            _vaults.Clear();
            foreach (var v in items)
                _vaults[v.VaultID] = v;
        }
    }
}
