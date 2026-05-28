using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace AVA.UPS.Adapter.Routing
{
    /// <summary>
    /// Central registry of all UPS-enabled modules known to this process.
    /// Tracks discovery metadata, diagnostics, and routing information.
    /// </summary>
    public class UPSModuleRegistry
    {
        private readonly ConcurrentDictionary<string, UPSModuleRecord> _modules =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> _discoveryErrors = new();

        public IEnumerable<UPSModuleRecord> AllModules => _modules.Values;

        public IReadOnlyList<string> DiscoveryErrors => _discoveryErrors;

        // ---------------------------------------------------------------------
        // Registration
        // ---------------------------------------------------------------------

        public void Register(UPSModuleInfo module, string? source = null, string? method = null)
        {
            if (module == null)
                throw new ArgumentNullException(nameof(module));

            if (string.IsNullOrWhiteSpace(module.Name))
            {
                AddDiscoveryError($"Module has no name (source: {source ?? "unknown"}).");
                return;
            }

            var record = new UPSModuleRecord
            {
                Info = module,
                Source = source ?? "runtime",
                Method = method ?? "manual",
                LoadedAt = DateTime.UtcNow
            };

            _modules[module.Name] = record;
        }

        public bool Unregister(string moduleName)
        {
            return _modules.TryRemove(moduleName, out _);
        }

        public void Clear()
        {
            _modules.Clear();
            _discoveryErrors.Clear();
        }

        // ---------------------------------------------------------------------
        // Resolve
        // ---------------------------------------------------------------------

        public UPSModuleRecord? Resolve(string moduleName)
        {
            if (string.IsNullOrWhiteSpace(moduleName))
                return null;

            _modules.TryGetValue(moduleName, out var record);
            return record;
        }

        public UPSModuleRecord ResolveOrThrow(string moduleName)
        {
            var record = Resolve(moduleName);
            if (record == null)
                throw new KeyNotFoundException($"Module '{moduleName}' not found in registry.");
            return record;
        }

        // ---------------------------------------------------------------------
        // Diagnostics
        // ---------------------------------------------------------------------

        public void AddDiscoveryError(string message)
        {
            lock (_discoveryErrors)
            {
                _discoveryErrors.Add(message);
            }
        }

        public string DumpAsJson()
        {
            var snapshot = new
            {
                modules = _modules.Values,
                errors = _discoveryErrors,
                timestamp = DateTime.UtcNow
            };

            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
    }

    /// <summary>
    /// Runtime metadata for a registry entry.
    /// </summary>
    /// <summary>
    /// Runtime metadata for a registry entry.
    /// </summary>
    public class UPSModuleRecord
    {
        public UPSModuleInfo Info { get; set; } = default!;
        public string Source { get; set; } = default!;
        public string Method { get; set; } = default!;
        public DateTime LoadedAt { get; set; }
        public DateTime LastUpdated { get; set; }

        // -------------------------------------------------------------
        // Backwards-compatible passthrough properties
        // -------------------------------------------------------------

        public string Name
        {
            get => Info.Name;
            set => Info.Name = value;
        }

        public string Transport
        {
            get => Info.Transport;
            set => Info.Transport = value;
        }

        public string? Endpoint
        {
            get => Info.Endpoint;
            set => Info.Endpoint = value;
        }

        public string? Version
        {
            get => Info.Version;
            set => Info.Version = value;
        }

        public string? Capabilities
        {
            get => Info.Capabilities;
            set => Info.Capabilities = value;
        }

        public bool Ephemeral
        {
            get => Info.Ephemeral;
            set => Info.Ephemeral = value;
        }
    }


    /// <summary>
    /// Declarative metadata for a UPS-capable module.
    /// </summary>
    public class UPSModuleInfo
    {
        public string Name { get; set; } = default!;
        public string Transport { get; set; } = default!;
        public string? Endpoint { get; set; }
        public string? Version { get; set; }
        public string? Capabilities { get; set; }
        public bool Ephemeral { get; set; } = false;
    }
}
