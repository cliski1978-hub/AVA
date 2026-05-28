using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using AVA.UPS.Adapter.Transport;

namespace AVA.UPS.Adapter
{
    /// <summary>
    /// Registry of all available protocol adapters for UPS routing.
    /// Supports initialization, diagnostics, configuration metadata,
    /// and thread-safe multi-transport resolution.
    /// </summary>
    public class AdapterRegistry
    {
        private readonly ConcurrentDictionary<string, AdapterRecord> _adapters =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _errors = new();

        public IReadOnlyList<string> Errors => _errors;

        public IEnumerable<AdapterRecord> All => _adapters.Values;

        // ------------------------------------------------------------
        // Registration
        // ------------------------------------------------------------
        public async Task RegisterAsync(
            IProtocolAdapter adapter,
            object? config = null,
            string? source = null)
        {
            if (adapter == null)
                throw new ArgumentNullException(nameof(adapter));

            var protocol = adapter.ProtocolName;

            try
            {
                await adapter.InitializeAsync(config);
            }
            catch (Exception ex)
            {
                AddError(
                    $"Adapter '{protocol}' failed initialization: {ex.Message}"
                );
                return;
            }

            var record = new AdapterRecord
            {
                Adapter = adapter,
                Protocol = protocol,
                Source = source ?? "runtime",
                LoadedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow,
                Config = config
            };

            _adapters[protocol] = record;
        }

        // Backward compatible registration (sync)
        public void Register(IProtocolAdapter adapter)
        {
            RegisterAsync(adapter).GetAwaiter().GetResult();
        }

        // ------------------------------------------------------------
        // Resolution
        // ------------------------------------------------------------
        public AdapterRecord? Resolve(string protocolName)
        {
            if (string.IsNullOrWhiteSpace(protocolName))
                return null;

            _adapters.TryGetValue(protocolName, out var record);
            return record;
        }

        public IProtocolAdapter? GetAdapter(string protocolName)
        {
            return Resolve(protocolName)?.Adapter;
        }

        // ------------------------------------------------------------
        // Diagnostics
        // ------------------------------------------------------------
        private void AddError(string message)
        {
            lock (_errors)
                _errors.Add(message);
        }

        public string DumpAsJson()
        {
            var snapshot = new
            {
                adapters = _adapters.Values,
                errors = _errors,
                timestamp = DateTime.UtcNow
            };

            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }

    /// <summary>
    /// Holds runtime metadata for a protocol adapter instance.
    /// </summary>
    public class AdapterRecord
    {
        public string Protocol { get; set; } = default!;
        public IProtocolAdapter Adapter { get; set; } = default!;
        public object? Config { get; set; }
        public string Source { get; set; } = default!;
        public DateTime LoadedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
