using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using AVA.Vault.Core.Config;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Graph;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Models;
using AVA.Vault.Core.Utils;

namespace AVA.Vault.Core.Services
{
    /// <summary>
    /// Handles export and import of complete Vaults for backup and migration.
    /// Bundles vault files into .zip archives containing notes, header, and config.
    /// </summary>
    public sealed class VaultExportService
    {
        private readonly VaultLogger _logger;

        public VaultExportService(VaultLogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // -------------------------------------------------------------
        // Export
        // -------------------------------------------------------------

        public async Task ExportVaultAsync(VaultService vault, string destinationZip)
        {
            _logger.Log(nameof(VaultExportService), $"Exporting vault {vault.Name} ? {destinationZip}");

            var tempDir = Path.Combine(Path.GetTempPath(), $"vault_export_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                // Build a simple header manually (since Config does not have ToHeader)
                var header = new VaultHeader
                {
                    ID = vault.Config.VaultID,
                    DisplayName = vault.Config.DisplayName,
                    OwnerId = "system",
                    Description = $"Exported vault: {vault.Name}",
                    CreatedAt = vault.Created,
                    LastSyncedAt = DateTime.UtcNow,
                    IsActive = true
                };

                // Save header and config
                VaultSerializer.SaveHeader(header, tempDir);
                VaultSerializer.SaveConfig(vault.Config, tempDir);

                // Save notes
                var notesDir = Path.Combine(tempDir, "notes");
                VaultMapper.ToMarkdownDirectory(vault.Notes, notesDir);

                // Save graph (optional)
                var graphFile = Path.Combine(tempDir, "graph.json");
                var graphJson = VaultSerializer.ToJson(vault.Graph);
                await File.WriteAllTextAsync(graphFile, graphJson);

                // Compress
                if (File.Exists(destinationZip))
                    File.Delete(destinationZip);
                ZipFile.CreateFromDirectory(tempDir, destinationZip, CompressionLevel.Fastest, false);

                _logger.Log(nameof(VaultExportService), $"Vault {vault.Name} exported successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(VaultExportService), "Vault export failed.", ex);
                throw;
            }
            finally
            {
                Directory.Delete(tempDir, recursive: true);
            }
        }

        // -------------------------------------------------------------
        // Import
        // -------------------------------------------------------------

        public async Task<VaultService> ImportVaultAsync(string zipPath, string targetDirectory)
        {
            _logger.Log(nameof(VaultExportService), $"Importing vault from {zipPath}");

            var extractPath = Path.Combine(Path.GetTempPath(), $"vault_import_{Guid.NewGuid():N}");
            ZipFile.ExtractToDirectory(zipPath, extractPath);

            try
            {
                var header = VaultSerializer.LoadHeader(extractPath);
                var config = VaultSerializer.LoadConfig(extractPath);

                var notes = VaultMapper.FromMarkdownDirectory(Path.Combine(extractPath, "notes"));

                var vault = new VaultService(header?.DisplayName ?? "ImportedVault")
                {
                    Config = config ?? new VaultInstanceConfig(),
                    Notes = notes
                };

                // Optional: Load graph if exists
                var graphFile = Path.Combine(extractPath, "graph.json");
                if (File.Exists(graphFile))
                {
                    var json = await File.ReadAllTextAsync(graphFile);
                    vault.Graph = VaultSerializer.FromJson<NoteGraph>(json) ?? new NoteGraph();
                }

                VaultDefaults.EnsureDirectories(targetDirectory);
                _logger.Log(nameof(VaultExportService), $"Vault {vault.Name} imported successfully.");
                return vault;
            }
            finally
            {
                Directory.Delete(extractPath, recursive: true);
            }
        }
    }
}
