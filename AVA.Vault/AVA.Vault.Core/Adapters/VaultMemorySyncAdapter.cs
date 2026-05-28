using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Data.Models;
using AVA.Memory.Core.Models;
using AVA.Memory.Data.Entities;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core.Adapters
{
    /// <summary>
    /// Synchronizes Vault note data to and from the connected MemoryCore instance.
    /// Handles one-way or bidirectional sync based on Vault configuration.
    /// Fully configurable and mock-mode compatible.
    /// </summary>
    public sealed class VaultMemorySyncAdapter : IVaultMemorySyncAdapter
    {
        private readonly IVaultDbContext _db;
        private readonly HttpClient _memoryClient;
        private readonly VaultLogger _logger;
        private readonly VaultInstanceConfig _config;

        // Internal property used by tests or diagnostics
        internal Uri MemoryEndpoint => _memoryClient.BaseAddress!;

        public VaultMemorySyncAdapter(
            IVaultDbContext db,
            VaultLogger logger,
            VaultInstanceConfig config,
            HttpClient? httpClient = null)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _config = config ?? throw new ArgumentNullException(nameof(config));

            _memoryClient = httpClient ?? new HttpClient
            {
                BaseAddress = new Uri(_config.MemoryEndpoint ?? "http://localhost:8082")
            };
        }

        // -------------------------------------------------------------
        // Public Sync Operations
        // -------------------------------------------------------------

        public async Task SyncAllAsync(CancellationToken ct = default)
        {
            if (_config.MockMode)
            {
                _logger.Log(nameof(VaultMemorySyncAdapter), "MockMode active: skipping Memory sync.");
                return;
            }

            _logger.Log(nameof(VaultMemorySyncAdapter), "Starting full Vault ? Memory synchronization...");
            await PushUnsentNotesAsync(ct);
            await PullFromMemoryAsync(ct);
        }

        public async Task PushToMemoryAsync(VaultNote note, CancellationToken ct = default)
        {
            if (_config.MockMode)
            {
                _logger.Log(nameof(VaultMemorySyncAdapter), $"MockMode: skipping push for {note.ID}");
                return;
            }

            try
            {
                var record = ConvertNoteToRecord(note);
                bool success = false;

                for (int attempt = 1; attempt <= 3 && !success; attempt++)
                {
                    var response = await _memoryClient.PostAsJsonAsync("/api/memory/records", record, ct);
                    if (response.IsSuccessStatusCode)
                    {
                        success = true;
                        _logger.Log(nameof(VaultMemorySyncAdapter), $"Synced note {note.ID} to MemoryCore.");
                        break;
                    }

                    await Task.Delay(500 * attempt, ct);
                }

                if (!success)
                    _logger.LogError(nameof(VaultMemorySyncAdapter), $"Failed after retries for note {note.ID}");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(VaultMemorySyncAdapter), $"Exception syncing note {note.ID}.", ex);
            }
        }

        public async Task PullFromMemoryAsync(CancellationToken ct = default)
        {
            if (_config.MockMode)
            {
                _logger.Log(nameof(VaultMemorySyncAdapter), "MockMode: skipping pull from MemoryCore.");
                return;
            }

            try
            {
                var lastUpdate = await _db.VaultNotes.AnyAsync(ct)
                    ? await _db.VaultNotes.MaxAsync(n => n.UpdatedAt, ct)
                    : DateTime.MinValue;

                var records = await _memoryClient.GetFromJsonAsync<List<MemoryRecord>>(
                    $"/api/memory/records?since={lastUpdate:o}", ct) ?? new();

                foreach (var record in records)
                {
                    var exists = await _db.VaultNotes.AnyAsync(n => n.ID == record.ID, ct);
                    if (!exists)
                    {
                        var note = ConvertRecordToNote(record);
                        _db.VaultNotes.Add(note);
                    }
                }

                await _db.FlushAsync(ct);
                _logger.Log(nameof(VaultMemorySyncAdapter), $"Pulled {records.Count} records from MemoryCore.");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(VaultMemorySyncAdapter), "Error pulling data from MemoryCore.", ex);
            }
        }

        // -------------------------------------------------------------
        // Local Sync Handling
        // -------------------------------------------------------------

        private async Task PushUnsentNotesAsync(CancellationToken ct)
        {
            var unsynced = await _db.VaultNotes
                .Where(n => n.IsSynced == false)
                .ToListAsync(ct);

            foreach (var note in unsynced)
            {
                await PushToMemoryAsync(note, ct);
                note.IsSynced = true;
            }

            await _db.FlushAsync(ct);
            _logger.Log(nameof(VaultMemorySyncAdapter), $"Pushed {unsynced.Count} unsynced notes to MemoryCore.");
        }

        // -------------------------------------------------------------
        // Mapping Utilities
        // -------------------------------------------------------------

        private static MemoryRecord ConvertNoteToRecord(VaultNote note)
        {
            return new MemoryRecord
            {
                ID = note.ID,
                Text = note.Content,
                // Uncomment when embeddings are active in AVA.Memory
                // Vector = note.Vector ?? Array.Empty<float>(),
                Metadata = new List<MemoryMetadata>
                {
                    new MemoryMetadata { Key = "VaultId", Value = note.VaultID },
                    new MemoryMetadata
                    {
                        Key = "Tags",
                        Value = string.Join(",", note.VaultNoteVaultTags?.Select(jt => jt.Tag.Name) ?? Array.Empty<string>())
                    }
                },
                Tags = note.VaultNoteVaultTags?.Select(jt => new MemoryTag { Tag = jt.Tag.Name }).ToList() ?? new List<MemoryTag>(),
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt,
                Source = "Vault"
            };
        }

        private static VaultNote ConvertRecordToNote(MemoryRecord record)
        {
            return new VaultNote
            {
                ID = record.ID,
                VaultID = record.Metadata?.FirstOrDefault(m => m.Key == "VaultId")?.Value ?? "unknown",
                Content = record.Text,
                VaultNoteVaultTags = record.Tags?.Select(t => new VaultNoteVaultTag
                {
                    ID = Guid.NewGuid().ToString(),
                    NoteID = record.ID,
                    TagID = Guid.NewGuid().ToString(),
                    Tag = new VaultTag { Name = t.Tag },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }).ToList() ?? new List<VaultNoteVaultTag>(),
                // Uncomment when embeddings are active in AVA.Memory
                // Vector = record.Vector ?? Array.Empty<float>(),
                CreatedAt = record.CreatedAt,
                UpdatedAt = record.UpdatedAt,
                IsSynced = true
            };
        }
    }
}
