using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AVA.UPS.Adapter.Contracts
{
    /// <summary>
    /// Central registry for all UPS JSON contract files.
    /// Handles loading, normalization, validation, and lookup.
    /// </summary>
    public class ContractRegistry
    {
        private readonly Dictionary<string, UPSContractFile> _modules = new();

        /// <summary>
        /// Exposes all loaded contract modules.
        /// Keys are canonical module names.
        /// </summary>
        public IReadOnlyDictionary<string, UPSContractFile> Modules => _modules;

        // Shared JSON options for all UPS contract loads
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            WriteIndented = false,
            Converters =
            {
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                // Additional converters may be added here
            }
        };

        // ----------------------------------------------------------------------------------------
        // Public API
        // ----------------------------------------------------------------------------------------

        /// <summary>
        /// Loads all UPS contract files from one or more directories.
        /// Supports deterministic ordering, module override rules, and full validation.
        /// </summary>
        public void LoadFromDirectories(params string[] directories)
        {
            if (directories == null || directories.Length == 0)
                throw new ArgumentException("No contract directories were provided.");

            foreach (var dir in directories)
                LoadFromDirectoryInternal(dir);
        }

        /// <summary>
        /// Loads all JSON contract files from a single directory.
        /// Applies strict validation and normalization.
        /// </summary>
        public void LoadFromDirectory(string directoryPath)
        {
            LoadFromDirectoryInternal(directoryPath);
        }

        /// <summary>
        /// Returns the contract for a specific method within a module,
        /// or null if the module or method is not registered.
        /// </summary>
        public UPSMethodContract? GetMethodContract(string module, string methodName)
        {
            if (!_modules.TryGetValue(module, out var file))
                return null;

            return file.Methods.FirstOrDefault(m =>
                string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase));
        }

        // ----------------------------------------------------------------------------------------
        // Internal Helpers
        // ----------------------------------------------------------------------------------------

        private void LoadFromDirectoryInternal(string directoryPath)
        {
            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"Contract directory not found: {directoryPath}");

            var jsonFiles = Directory
                .GetFiles(directoryPath, "*.json")
                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var file in jsonFiles)
                LoadContractFile(file);
        }

        private void LoadContractFile(string filePath)
        {
            string json = File.ReadAllText(filePath);

            var contract = JsonSerializer.Deserialize<UPSContractFile>(json, JsonOptions);
            if (contract == null)
                throw new Exception($"Invalid UPS contract: {filePath}");

            NormalizeContract(contract);
            ValidateContract(contract, filePath);

            if (_modules.ContainsKey(contract.Module))
            {
                // Allow override with a warning or throw depending on future settings.
                // For now: throw to prevent silent corruption.
                throw new Exception($"Duplicate UPS module detected: {contract.Module} at {filePath}");
            }

            _modules[contract.Module] = contract;
        }

        // ----------------------------------------------------------------------------------------
        // Normalization & Validation
        // ----------------------------------------------------------------------------------------

        private static void NormalizeContract(UPSContractFile contract)
        {
            contract.Module = contract.Module?.Trim() ?? string.Empty;
            contract.Version = contract.Version?.Trim() ?? "1.0.0";

            foreach (var method in contract.Methods)
            {
                method.Name = method.Name?.Trim() ?? string.Empty;

                foreach (var param in method.Parameters)
                {
                    param.Key = param.Key?.Trim() ?? string.Empty;
                    param.Type = param.Type?.Trim() ?? string.Empty;
                }
            }
        }

        private static void ValidateContract(UPSContractFile contract, string filePath)
        {
            if (string.IsNullOrWhiteSpace(contract.Module))
                throw new Exception($"Contract missing module name: {filePath}");

            if (contract.Methods == null)
                throw new Exception($"Contract contains no methods: {contract.Module}");

            var duplicateMethods = contract.Methods
                .GroupBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateMethods.Count > 0)
                throw new Exception($"Duplicate method names in contract for module {contract.Module}: {string.Join(", ", duplicateMethods)}");

            foreach (var method in contract.Methods)
            {
                if (string.IsNullOrWhiteSpace(method.Name))
                    throw new Exception($"Method with empty or null name in module {contract.Module}");

                foreach (var param in method.Parameters)
                {
                    if (string.IsNullOrWhiteSpace(param.Key))
                        throw new Exception($"Method {method.Name} in module {contract.Module} has a parameter with an empty key.");

                    if (string.IsNullOrWhiteSpace(param.Type))
                        throw new Exception($"Parameter '{param.Key}' in {method.Name} / {contract.Module} missing Type.");
                }
            }
        }
    }
}
