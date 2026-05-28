using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Registry;
using AVA.Vault.Core.Logger;

namespace AVA.Vault.Core.Adapters
{
    /// <summary>
    /// Provides synchronization between the local VaultRegistry and a remote endpoint or database.
    /// Handles publishing, retrieval, and reconciliation of VaultInstance data across distributed nodes.
    /// </summary>
    public sealed class VaultRegistryAdapter
    {
        private readonly VaultRegistry _localRegistry;
        private readonly HttpClient _httpClient;
        private readonly VaultLogger _logger;
        private readonly string _remoteEndpoint;

        public VaultRegistryAdapter(VaultRegistry localRegistry, string remoteEndpoint, VaultLogger logger)
        {
            _localRegistry = localRegistry ?? throw new ArgumentNullException(nameof(localRegistry));
            _remoteEndpoint = remoteEndpoint ?? throw new ArgumentNullException(nameof(remoteEndpoint));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
        }

        // -------------------------------------------------------------
        // Push local registry to remote endpoint
        // -------------------------------------------------------------

        public async Task PushAsync(CancellationToken ct = default)
        {
            var vaults = await _localRegistry.GetAllVaultsAsync(ct);
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{_remoteEndpoint}/api/vaults/sync", vaults, ct);
                if (response.IsSuccessStatusCode)
                {
                    _logger.Log("VaultRegistryAdapter", $"Pushed {vaults.Count} vaults to remote registry.");
                }
                else
                {
                    _logger.LogError("VaultRegistryAdapter", $"Failed to push vault registry: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("VaultRegistryAdapter", "Error pushing registry.", ex);
            }
        }

        // -------------------------------------------------------------
        // Pull latest registry snapshot from remote
        // -------------------------------------------------------------

        public async Task PullAsync(CancellationToken ct = default)
        {
            try
            {
                var remoteVaults = await _httpClient.GetFromJsonAsync<List<VaultInstance>>(
                    $"{_remoteEndpoint}/api/vaults", ct);

                if (remoteVaults == null)
                {
                    _logger.Log("VaultRegistryAdapter", "No remote registry data available.");
                    return;
                }

                await SyncLocalRegistry(remoteVaults, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError("VaultRegistryAdapter", "Error pulling remote registry.", ex);
            }
        }

        // -------------------------------------------------------------
        // Sync local registry with remote state
        // -------------------------------------------------------------

        private async Task SyncLocalRegistry(IEnumerable<VaultInstance> remoteVaults, CancellationToken ct)
        {
            var localVaults = await _localRegistry.GetAllVaultsAsync(ct);
            var localSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var v in localVaults) localSet.Add(v.VaultID);

            int added = 0;
            foreach (var remote in remoteVaults)
            {
                if (!localSet.Contains(remote.VaultID))
                {
                    await _localRegistry.RegisterVaultAsync(remote, ct);
                    added++;
                }
            }

            _logger.Log("VaultRegistryAdapter", $"Synchronized {added} new vaults from remote registry.");
        }
    }
}
