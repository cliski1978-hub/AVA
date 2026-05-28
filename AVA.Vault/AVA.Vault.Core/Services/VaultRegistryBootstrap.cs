using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Registry;
using AVA.Vault.Core.Adapters;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Bootstraps the Vault registry subsystem using configuration from VaultRegistryConfig.json.
    /// Responsible for loading registry config, initializing adapters, and managing sync intervals.
    /// </summary>
    public sealed class VaultRegistryBootstrap : IAsyncDisposable
    {
        private readonly VaultRegistry _registry;
        private readonly VaultLogger _logger;
        private VaultRegistryAdapter? _adapter;
        private VaultRegistryConfig? _config;
        private Timer? _syncTimer;
        private bool _isInitialized;

        public VaultRegistry Registry => _registry;
        public VaultRegistryConfig? Config => _config;

        public VaultRegistryBootstrap(VaultRegistry registry, VaultLogger logger)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // -------------------------------------------------------------
        // Initialization
        // -------------------------------------------------------------

        public async Task InitializeAsync(string configPath = "VaultRegistryConfig.json", CancellationToken ct = default)
        {
            if (_isInitialized)
                return;

            _config = LoadConfig(configPath);

            _logger.Log("VaultRegistryBootstrap", $"Initializing Vault Registry (Mode: {_config.Mode})");

            if (_config.Mode != VaultRegistryMode.Local && !string.IsNullOrWhiteSpace(_config.RemoteEndpoint))
            {
                _adapter = new VaultRegistryAdapter(_registry, _config.RemoteEndpoint, _logger);
                if (_config.EnableAutoSync)
                {
                    StartAutoSync(TimeSpan.FromSeconds(_config.SyncIntervalSeconds));
                }

                if (_config.Mode == VaultRegistryMode.Hybrid)
                {
                    _logger.Log("VaultRegistryBootstrap", "Hybrid mode: Pulling remote vault registry snapshot...");
                    await _adapter.PullAsync(ct);
                }
            }

            _isInitialized = true;
        }

        // -------------------------------------------------------------
        // Config Loader
        // -------------------------------------------------------------

        private VaultRegistryConfig LoadConfig(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    _logger.Log("VaultRegistryBootstrap", $"Registry config not found at {path}, using defaults.");
                    var defaultConfig = new VaultRegistryConfig();
                    File.WriteAllText(path, JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions { WriteIndented = true }));
                    return defaultConfig;
                }

                var json = File.ReadAllText(path);
                var config = JsonSerializer.Deserialize<VaultRegistryConfig>(json);

                if (config == null)
                    throw new InvalidOperationException("Failed to parse registry configuration.");

                return config;
            }
            catch (Exception ex)
            {
                _logger.LogError("VaultRegistryBootstrap", "Failed to load registry config.", ex);
                return new VaultRegistryConfig();
            }
        }

        // -------------------------------------------------------------
        // Synchronization Management
        // -------------------------------------------------------------

        private void StartAutoSync(TimeSpan interval)
        {
            _syncTimer = new Timer(async _ => await PerformSyncAsync(), null, TimeSpan.Zero, interval);
            _logger.Log("VaultRegistryBootstrap", $"Auto-sync timer started: every {interval.TotalSeconds} seconds.");
        }

        private async Task PerformSyncAsync()
        {
            if (_adapter == null)
                return;

            try
            {
                await _adapter.PushAsync();
                await _adapter.PullAsync();
                _logger.Log("VaultRegistryBootstrap", "Registry synchronization cycle completed.");
            }
            catch (Exception ex)
            {
                _logger.LogError("VaultRegistryBootstrap", "Error during registry sync cycle.", ex);
            }
        }

        // -------------------------------------------------------------
        // Cleanup
        // -------------------------------------------------------------

        public async ValueTask DisposeAsync()
        {
            _syncTimer?.Dispose();
            if (_adapter != null)
            {
                _logger.Log("VaultRegistryBootstrap", "Stopping registry adapter.");
                await _adapter.PushAsync();
            }
        }
    }
}
