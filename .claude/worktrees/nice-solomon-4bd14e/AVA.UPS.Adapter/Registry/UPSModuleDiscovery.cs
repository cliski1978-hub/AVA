using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using AVA.UPS.Adapter.Routing;

namespace AVA.UPS.Adapter.Registry
{
    /// <summary>
    /// Discovers UPS modules from JSON configuration files,
    /// environment variables, directory scans, and future distributed sources.
    /// </summary>
    public static class UPSModuleDiscovery
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // ---------------------------------------------------------------------
        // High-Level Loaders
        // ---------------------------------------------------------------------

        /// <summary>
        /// Loads module definitions from a single JSON file.
        /// Adds them into the registry with discovery metadata.
        /// </summary>
        public static void LoadFromJsonFile(string filePath, UPSModuleRegistry registry)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Module discovery file not found: {filePath}");

            string json;
            try
            {
                json = File.ReadAllText(filePath);
            }
            catch (Exception ex)
            {
                registry.AddDiscoveryError($"Failed to read discovery file '{filePath}': {ex.Message}");
                return;
            }

            UPSModuleDiscoveryFile? root;
            try
            {
                root = JsonSerializer.Deserialize<UPSModuleDiscoveryFile>(json, JsonOptions);
            }
            catch (Exception ex)
            {
                registry.AddDiscoveryError($"Invalid JSON in '{filePath}': {ex.Message}");
                return;
            }

            if (root?.Modules == null)
            {
                registry.AddDiscoveryError($"Discovery JSON missing 'modules' array: {filePath}");
                return;
            }

            foreach (var module in root.Modules)
                RegisterModule(registry, module, filePath, "json-file");
        }

        /// <summary>
        /// Loads all *.json files from a directory, applying layered config loading.
        /// </summary>
        public static void LoadFromDirectory(string directory, UPSModuleRegistry registry)
        {
            if (!Directory.Exists(directory))
                throw new DirectoryNotFoundException($"Module discovery directory not found: {directory}");

            var files = Directory.GetFiles(directory, "*.json")
                .OrderBy(f => f) // deterministic load order
                .ToList();

            foreach (var file in files)
                LoadFromJsonFile(file, registry);
        }

        /// <summary>
        /// Loads modules from environment variables:
        /// UPS_MODULES = "VaultService|http://host:9000, Identity|grpc://host:5001"
        /// </summary>
        public static void LoadFromEnvironment(UPSModuleRegistry registry)
        {
            var env = Environment.GetEnvironmentVariable("UPS_MODULES");
            if (string.IsNullOrWhiteSpace(env))
                return;

            var entries = env.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                // Format: name|endpoint
                var parts = entry.Split('|');
                if (parts.Length < 2)
                {
                    registry.AddDiscoveryError($"Invalid UPS_MODULES entry: {entry}");
                    continue;
                }

                var info = new UPSModuleInfo
                {
                    Name = parts[0].Trim(),
                    Endpoint = parts[1].Trim(),
                    Transport = InferTransport(parts[1].Trim())
                };

                RegisterModule(registry, info, "ENV:UPS_MODULES", "environment");
            }
        }

        // ---------------------------------------------------------------------
        // Internal Helpers
        // ---------------------------------------------------------------------

        private static void RegisterModule(
            UPSModuleRegistry registry,
            UPSModuleInfo module,
            string source,
            string method)
        {
            if (!ValidateModule(module, registry, source))
                return;

            registry.Register(module, source, method);
        }

        private static bool ValidateModule(
            UPSModuleInfo module,
            UPSModuleRegistry registry,
            string source)
        {
            var errs = new List<string>();

            if (string.IsNullOrWhiteSpace(module.Name))
                errs.Add("Missing module name.");

            if (string.IsNullOrWhiteSpace(module.Endpoint))
                errs.Add("Missing endpoint.");

            if (string.IsNullOrWhiteSpace(module.Transport))
                errs.Add("Missing transport.");

            if (errs.Count > 0)
            {
                registry.AddDiscoveryError(
                    $"Module validation failed ({source}): {module.Name ?? "<unknown>"} → {string.Join("; ", errs)}"
                );
                return false;
            }

            return true;
        }

        private static string InferTransport(string endpoint)
        {
            // Auto-detect transport based on URI schema
            if (endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return "http";

            if (endpoint.StartsWith("grpc", StringComparison.OrdinalIgnoreCase))
                return "grpc";

            if (endpoint.StartsWith("tcp", StringComparison.OrdinalIgnoreCase))
                return "tcp";

            return "custom";
        }
    }

    /// <summary>
    /// Helper model for JSON discovery file deserialization.
    /// </summary>
    internal class UPSModuleDiscoveryFile
    {
        public List<UPSModuleInfo> Modules { get; set; } = new();
    }
}
