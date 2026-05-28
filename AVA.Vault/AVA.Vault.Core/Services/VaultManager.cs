using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Models;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Manages the lifecycle and storage of vaults on disk.
    /// Responsible for creating, loading, listing, and deleting vaults.
    /// </summary>
    public class VaultManager : IVaultManager
    {
        private readonly Dictionary<string, VaultService> _vaults = new();
        private readonly string _vaultsRoot;

        public VaultManager(string vaultsRoot = "Vaults")
        {
            _vaultsRoot = vaultsRoot;
            Directory.CreateDirectory(_vaultsRoot);
        }

        // -------------------------------------------------------------
        // Vault Creation
        // -------------------------------------------------------------

        public VaultService CreateVault(string name)
        {
            // ?? Temporarily generate a GUID until AVA.Identity assigns VaultIDs
            var vaultId = Guid.NewGuid().ToString();
            var vaultPath = Path.Combine(_vaultsRoot, name);
            Directory.CreateDirectory(vaultPath);

            var vault = new VaultService(name)
            {
                Id = vaultId,
                Created = DateTime.UtcNow
            };

            _vaults[vaultId] = vault;

            // Create VaultHeader (used by both SQL + JSON)
            var header = new VaultHeader
            {
                ID = vault.Id,
                DisplayName = vault.Name,
                CreatedAt = vault.Created,
                IsActive = true
            };

            SaveHeader(header, vaultPath);
            return vault;
        }

        // -------------------------------------------------------------
        // Vault Loading
        // -------------------------------------------------------------

        public VaultService LoadVault(string path)
        {
            var header = LoadHeader(path);
            var vault = new VaultService(header.DisplayName)
            {
                Id = header.ID,
                Created = header.CreatedAt
            };

            _vaults[header.ID] = vault;
            return vault;
        }

        // -------------------------------------------------------------
        // Query Operations
        // -------------------------------------------------------------

        public VaultService? GetVaultById(string vaultId) =>
            _vaults.TryGetValue(vaultId, out var vault) ? vault : null;

        public IEnumerable<VaultHeader> ListVaults()
        {
            foreach (var dir in Directory.GetDirectories(_vaultsRoot))
            {
                var headerPath = Path.Combine(dir, "vault.header.json");
                if (!File.Exists(headerPath))
                    continue;

                VaultHeader? header = null;

                try
                {
                    var json = File.ReadAllText(headerPath);
                    header = JsonSerializer.Deserialize<VaultHeader>(json, ReadOptions);
                }
                catch
                {
                    // skip unreadable or corrupt headers
                }

                if (header != null)
                    yield return header;
            }
        }


        // -------------------------------------------------------------
        // Deletion
        // -------------------------------------------------------------

        public void DeleteVault(string vaultId)
        {
            if (!_vaults.TryGetValue(vaultId, out var vault))
                return;

            var vaultPath = Path.Combine(_vaultsRoot, vault.Name);
            if (Directory.Exists(vaultPath))
                Directory.Delete(vaultPath, recursive: true);

            _vaults.Remove(vaultId);
        }

        // -------------------------------------------------------------
        // Internal Helpers
        // -------------------------------------------------------------

        private void SaveHeader(VaultHeader header, string path)
        {
            var json = JsonSerializer.Serialize(header, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(Path.Combine(path, "vault.header.json"), json);
        }

        private static readonly JsonSerializerOptions ReadOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private VaultHeader LoadHeader(string path)
        {
            var headerFile = Path.Combine(path, "vault.header.json");
            if (!File.Exists(headerFile))
                throw new FileNotFoundException("Vault header not found.", headerFile);

            var json = File.ReadAllText(headerFile);
            var header = JsonSerializer.Deserialize<VaultHeader>(json, ReadOptions);

            if (header == null)
                throw new InvalidOperationException($"Failed to deserialize vault header at: {path}");

            return header;
        }
    }
}
