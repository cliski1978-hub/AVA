using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Config;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using Microsoft.EntityFrameworkCore;

namespace AVA.Vault.Core.Adapters
{
    /// <summary>
    /// Handles all Vault file operations: import/export, attachments,
    /// backups, manifest generation, validation, and folder management.
    /// </summary>
    public sealed class VaultFileAdapter : IVaultFileAdapter
    {
        private readonly IVaultDbContext _db;
        private readonly VaultLogger _logger;
        private readonly VaultInstanceConfig _config;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        // Root vault directory
        private string VaultRoot => Path.GetFullPath(Path.GetDirectoryName(_config.StoragePath) ?? ".");

        public VaultFileAdapter(IVaultDbContext db, VaultLogger logger, VaultInstanceConfig config)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));
        }

        // =====================================================================
        // EXPORT
        // =====================================================================
        public async Task<string> ExportVaultAsync(string outputPath, CancellationToken ct = default)
        {
            if (_config.MockMode)
            {
                _logger.Log(nameof(VaultFileAdapter), "MockMode active: skipping vault export.");
                return "mock_export.vault";
            }

            try
            {
                var exportDir = Path.Combine(VaultRoot, "temp", $"export_{_config.VaultID}");
                Directory.CreateDirectory(exportDir);

                // Export all EF tables
                await WriteJsonAsync(exportDir, "notes.json", await _db.VaultNotes.ToListAsync(ct));
                await WriteJsonAsync(exportDir, "tags.json", await _db.VaultTags.ToListAsync(ct));
                await WriteJsonAsync(exportDir, "relations.json", await _db.VaultNoteRelations.ToListAsync(ct));
                await WriteJsonAsync(exportDir, "graphs.json", await _db.VaultGraphs.ToListAsync(ct));
                await WriteJsonAsync(exportDir, "projects.json", await _db.VaultProjects.ToListAsync(ct));
                await WriteJsonAsync(exportDir, "metadata.json", await _db.VaultMetadata.ToListAsync(ct));

                await ExportChatTranscriptsAsync(exportDir, ct);

                // Manifest generation
                var manifest = await GenerateManifestAsync(exportDir, ct);

                await File.WriteAllTextAsync(
                    Path.Combine(exportDir, "manifest.json"),
                    JsonSerializer.Serialize(manifest, _jsonOptions),
                    ct);

                Directory.CreateDirectory(outputPath);

                var vaultFile = Path.Combine(outputPath,
                    $"{_config.DisplayName}_{_config.VaultID}.vault");

                if (File.Exists(vaultFile))
                    File.Delete(vaultFile);

                ZipFile.CreateFromDirectory(exportDir, vaultFile, CompressionLevel.Optimal, false);
                Directory.Delete(exportDir, true);

                _logger.Log(nameof(VaultFileAdapter), $"Vault exported: {vaultFile}");
                return vaultFile;
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(VaultFileAdapter), "Error exporting vault.", ex);
                throw;
            }
        }

        // =====================================================================
        // IMPORT
        // =====================================================================
        public async Task ImportVaultAsync(string vaultFilePath, CancellationToken ct = default)
        {
            if (_config.MockMode)
            {
                _logger.Log(nameof(VaultFileAdapter), "MockMode active: skipping vault import.");
                return;
            }

            if (!File.Exists(vaultFilePath))
                throw new FileNotFoundException("Vault file not found.", vaultFilePath);

            try
            {
                var tempDir = Path.Combine(VaultRoot, "temp", $"import_{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempDir);

                ZipFile.ExtractToDirectory(vaultFilePath, tempDir, overwriteFiles: true);

                if (!await ValidateManifestAsync(tempDir, ct))
                    throw new InvalidDataException("Vault package failed integrity validation.");

                async Task<T?> LoadAsync<T>(string name) where T : class
                {
                    var path = Path.Combine(tempDir, name);
                    return File.Exists(path)
                        ? JsonSerializer.Deserialize<T>(
                              await File.ReadAllTextAsync(path, ct),
                              _jsonOptions)
                        : null;
                }

                var notes = await LoadAsync<List<VaultNote>>("notes.json") ?? new();
                var tags = await LoadAsync<List<VaultTag>>("tags.json") ?? new();
                var relations = await LoadAsync<List<VaultNoteRelation>>("relations.json") ?? new();
                var graphs = await LoadAsync<List<VaultGraph>>("graphs.json") ?? new();
                var projects = await LoadAsync<List<VaultProject>>("projects.json") ?? new();
                var metadata = await LoadAsync<List<VaultMetadata>>("metadata.json") ?? new();

                // Clear existing data
                _db.VaultNotes.RemoveRange(await _db.VaultNotes.ToListAsync(ct));
                _db.VaultTags.RemoveRange(await _db.VaultTags.ToListAsync(ct));
                _db.VaultNoteRelations.RemoveRange(await _db.VaultNoteRelations.ToListAsync(ct));
                _db.VaultGraphs.RemoveRange(await _db.VaultGraphs.ToListAsync(ct));
                _db.VaultProjects.RemoveRange(await _db.VaultProjects.ToListAsync(ct));
                _db.VaultMetadata.RemoveRange(await _db.VaultMetadata.ToListAsync(ct));

                // Add imported
                await _db.VaultNotes.AddRangeAsync(notes, ct);
                await _db.VaultTags.AddRangeAsync(tags, ct);
                await _db.VaultNoteRelations.AddRangeAsync(relations, ct);
                await _db.VaultGraphs.AddRangeAsync(graphs, ct);
                await _db.VaultProjects.AddRangeAsync(projects, ct);
                await _db.VaultMetadata.AddRangeAsync(metadata, ct);

                await _db.FlushAsync(ct);

                Directory.Delete(tempDir, true);

                _logger.Log(nameof(VaultFileAdapter), $"Vault import complete: {vaultFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(VaultFileAdapter), "Error importing vault.", ex);
                throw;
            }
        }

        // =====================================================================
        // ATTACHMENTS + CHAT EXPORT
        // =====================================================================
        private async Task ExportChatTranscriptsAsync(string exportDir, CancellationToken ct)
        {
            var chatNotes = await _db.VaultNotes
                .Where(n => n.VaultNoteVaultTags.Any(jt => jt.Tag.Name.ToLower() == "chat"))
                .ToListAsync(ct);

            var chatDir = Path.Combine(exportDir, "chats");
            Directory.CreateDirectory(chatDir);

            foreach (var note in chatNotes)
            {
                var file = Path.Combine(chatDir, $"{note.ID}_transcript.md");
                var content = $"# {note.Title}\n\n{note.Content}";
                await File.WriteAllTextAsync(file, content, ct);
            }
        }

        public async Task<string> SaveAttachmentAsync(string noteId, Stream file, string fileName,
            CancellationToken ct = default)
        {
            var dir = Path.Combine(VaultRoot, "attachments", noteId);
            Directory.CreateDirectory(dir);

            var fullPath = Path.Combine(dir, fileName);

            await using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await file.CopyToAsync(fs, ct);

            _logger.Log(nameof(VaultFileAdapter), $"Attachment saved: {fileName}");
            return fullPath;
        }

        public Task<Stream?> GetAttachmentAsync(string noteId, string fileName, CancellationToken ct = default)
        {
            var path = Path.Combine(VaultRoot, "attachments", noteId, fileName);

            Stream? result = File.Exists(path)
                ? (Stream)File.OpenRead(path)
                : null;

            return Task.FromResult(result);
        }


        public Task DeleteAttachmentAsync(string noteId, string fileName, CancellationToken ct = default)
        {
            var path = Path.Combine(VaultRoot, "attachments", noteId, fileName);

            if (File.Exists(path))
                File.Delete(path);

            _logger.Log(nameof(VaultFileAdapter), $"Attachment deleted: {fileName}");
            return Task.CompletedTask;
        }

        // =====================================================================
        // MOVE VAULT
        // =====================================================================
        public async Task<string> MoveVaultAsync(string newPath, CancellationToken ct = default)
        {
            var source = VaultRoot;

            if (!Directory.Exists(source))
                throw new DirectoryNotFoundException($"Vault directory not found: {source}");

            var target = Path.GetFullPath(newPath);
            Directory.CreateDirectory(target);

            foreach (var dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dir.Replace(source, target));

            foreach (var file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
                File.Copy(file, file.Replace(source, target), overwrite: true);

            _config.StoragePath = Path.Combine(target, Path.GetFileName(_config.StoragePath));

            _logger.Log(nameof(VaultFileAdapter), $"Vault moved ? {target}");
            await Task.CompletedTask;

            return target;
        }

        // =====================================================================
        // BACKUP VAULT
        // =====================================================================
        public async Task BackupVaultAsync(string targetDir, CancellationToken ct = default)
        {
            Directory.CreateDirectory(targetDir);

            var backupFile = Path.Combine(
                targetDir,
                $"vault_backup_{_config.VaultID}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip");

            if (File.Exists(backupFile))
                File.Delete(backupFile);

            ZipFile.CreateFromDirectory(VaultRoot, backupFile, CompressionLevel.Optimal, includeBaseDirectory: false);

            _logger.Log(nameof(VaultFileAdapter), $"Backup created ? {backupFile}");
            await Task.CompletedTask;
        }

        // =====================================================================
        // PURGE TEMP FILES
        // =====================================================================
        public async Task PurgeTempFilesAsync(CancellationToken ct = default)
        {
            var tempDir = Path.Combine(VaultRoot, "temp");

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
                _logger.Log(nameof(VaultFileAdapter), "Temp files purged.");
            }

            await Task.CompletedTask;
        }

        // =====================================================================
        // VALIDATE STRUCTURE
        // =====================================================================
        public async Task<bool> ValidateVaultStructureAsync(CancellationToken ct = default)
        {
            var dirs = new[]
            {
                VaultRoot,
                Path.Combine(VaultRoot, "attachments"),
                Path.Combine(VaultRoot, "temp"),
            };

            foreach (var dir in dirs)
            {
                if (!Directory.Exists(dir))
                {
                    _logger.LogError(nameof(VaultFileAdapter), $"Missing directory: {dir}");
                    return false;
                }
            }

            await Task.CompletedTask;
            return true;
        }

        // =====================================================================
        // HASH + VERIFY
        // =====================================================================
        private static string ComputeFileHashInternal(string path)
        {
            using var sha = SHA256.Create();
            var bytes = File.ReadAllBytes(path);
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLower();
        }

        public Task<string> ComputeFileHashAsync(string path, CancellationToken ct = default)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("File not found for hashing.", path);

            return Task.FromResult(ComputeFileHashInternal(path));
        }

        public async Task<bool> VerifyFileIntegrityAsync(
            string path,
            string expectedHash,
            CancellationToken ct = default)
        {
            if (!File.Exists(path))
                return false;

            var actual = await ComputeFileHashAsync(path, ct);
            return string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase);
        }

        // =====================================================================
        // MANIFEST
        // =====================================================================
        private async Task<VaultManifest> GenerateManifestAsync(string folder, CancellationToken ct)
        {
            var manifest = new VaultManifest
            {
                VaultID = _config.VaultID,
                DisplayName = _config.DisplayName,
                ExportedAt = DateTime.UtcNow.ToString("O"),
                SchemaVersion = "1.0",
                FileChecksums = new()
            };

            foreach (var file in Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories))
            {
                var rel = file.Replace(folder + Path.DirectorySeparatorChar, "");
                manifest.FileChecksums[rel] = ComputeFileHashInternal(file);
            }

            await Task.CompletedTask;
            return manifest;
        }

        private async Task<bool> ValidateManifestAsync(string folder, CancellationToken ct)
        {
            var manifestPath = Path.Combine(folder, "manifest.json");
            if (!File.Exists(manifestPath)) return false;

            var manifest = JsonSerializer.Deserialize<VaultManifest>(
                await File.ReadAllTextAsync(manifestPath, ct),
                _jsonOptions);

            if (manifest == null || manifest.FileChecksums.Count == 0)
                return false;

            foreach (var (relative, expected) in manifest.FileChecksums)
            {
                var full = Path.Combine(folder, relative);

                if (!File.Exists(full))
                    return false;

                var actual = ComputeFileHashInternal(full);
                if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        private static async Task WriteJsonAsync<T>(string dir, string name, T data)
        {
            var path = Path.Combine(dir, name);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(data, _jsonOptions));
        }
    }

    /// <summary>
    /// Manifest used for vault validation.
    /// </summary>
    public sealed class VaultManifest
    {
        public string VaultID { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ExportedAt { get; set; } = DateTime.UtcNow.ToString("O");
        public string SchemaVersion { get; set; } = "1.0";
        public Dictionary<string, string> FileChecksums { get; set; } = new();
    }
}
