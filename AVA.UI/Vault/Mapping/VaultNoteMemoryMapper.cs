using AVA.Memory.Abstractions.Models;
using AVA.Vault.Core.Data.Models;

namespace AVA.UI.Vault.Mapping;

/// <summary>
/// Maps VaultNote entities to MemoryRecordDto for direct in-process Memory persistence.
/// Used by VaultUiSyncService — no HTTP involved.
/// </summary>
public static class VaultNoteMemoryMapper
{
    /// <summary>
    /// Converts a VaultNote into a MemoryRecordDto suitable for IMemoryStore.UpsertAsync.
    /// Tags and metadata are preserved. Vectors are empty until an embedding provider is wired.
    /// </summary>
    public static MemoryRecordDto ToMemoryRecord(VaultNote note)
    {
        var now = DateTime.UtcNow;

        var tags = note.VaultNoteVaultTags?
            .Select(jt => new MemoryTagDto
            {
                RecordID              = note.ID,
                Tag                   = jt.Tag.Name,
                CreatedAt             = now,
                UpdatedAt             = now,
                PrimaryIdentityId     = note.PrimaryIdentityId ?? string.Empty,
                PrimaryIdentityHandle = note.PrimaryIdentityHandle ?? string.Empty,
                PrimaryIdentityType   = note.PrimaryIdentityType ?? "system"
            })
            .ToList() ?? new List<MemoryTagDto>();

        var metadata = new List<MemoryMetadataDto>
        {
            new()
            {
                RecordID              = note.ID,
                Key                   = "VaultID",
                Value                 = note.VaultID,
                CreatedAt             = now,
                UpdatedAt             = now,
                PrimaryIdentityId     = note.PrimaryIdentityId ?? string.Empty,
                PrimaryIdentityHandle = note.PrimaryIdentityHandle ?? string.Empty,
                PrimaryIdentityType   = note.PrimaryIdentityType ?? "system"
            },
            new()
            {
                RecordID              = note.ID,
                Key                   = "ProjectID",
                Value                 = note.ProjectID,
                CreatedAt             = now,
                UpdatedAt             = now,
                PrimaryIdentityId     = note.PrimaryIdentityId ?? string.Empty,
                PrimaryIdentityHandle = note.PrimaryIdentityHandle ?? string.Empty,
                PrimaryIdentityType   = note.PrimaryIdentityType ?? "system"
            },
            new()
            {
                RecordID              = note.ID,
                Key                   = "Title",
                Value                 = note.Title,
                CreatedAt             = now,
                UpdatedAt             = now,
                PrimaryIdentityId     = note.PrimaryIdentityId ?? string.Empty,
                PrimaryIdentityHandle = note.PrimaryIdentityHandle ?? string.Empty,
                PrimaryIdentityType   = note.PrimaryIdentityType ?? "system"
            }
        };

        return new MemoryRecordDto
        {
            ID                    = note.ID,
            Text                  = note.Content,
            Source                = "Vault",
            ContextId             = note.ProjectID,
            EpisodeId             = note.VaultID,
            Tags                  = tags,
            Metadata              = metadata,
            Vectors               = new List<MemoryVectorDto>(), // populated by embedding provider later
            Salience              = 1.0,
            Novelty               = 1.0,
            Frequency             = 1.0,
            DecayRate             = 0.01,
            CreatedAt             = note.CreatedAt,
            UpdatedAt             = note.UpdatedAt,
            LastAccessedAt        = DateTime.UtcNow,
            PrimaryIdentityId     = note.PrimaryIdentityId ?? string.Empty,
            PrimaryIdentityHandle = note.PrimaryIdentityHandle ?? string.Empty,
            PrimaryIdentityType   = note.PrimaryIdentityType ?? "system"
        };
    }
}
