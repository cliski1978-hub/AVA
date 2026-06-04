using System;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultNoteContextReadService : IVaultNoteContextReadService
    {
        private readonly IVaultNoteDetailsReadService _noteDetailsRead;
        private readonly IVaultNoteUsageReadService _noteUsageRead;
        private readonly VaultLogger _logger;

        public VaultNoteContextReadService(
            IVaultNoteDetailsReadService noteDetailsRead,
            IVaultNoteUsageReadService noteUsageRead,
            VaultLogger logger)
        {
            _noteDetailsRead = noteDetailsRead ?? throw new ArgumentNullException(nameof(noteDetailsRead));
            _noteUsageRead = noteUsageRead ?? throw new ArgumentNullException(nameof(noteUsageRead));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultNoteContextDto?> GetNoteContextAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var details = await _noteDetailsRead.GetNoteDetailsAsync(noteId, ct);
            if (details == null)
                return null;

            var usage = await _noteUsageRead.GetNoteUsageAsync(noteId, ct);

            return new VaultNoteContextDto
            {
                NoteID = details.NoteID,
                VaultID = details.VaultID,
                SessionID = details.SessionID,
                Title = details.Title,
                Summary = details.Summary,
                Content = details.Content,
                IsPinned = details.IsPinned,
                IsTemplate = details.IsTemplate,
                MetadataCount = details.Metadata.Count,
                TagCount = details.Tags.Count,
                FileCount = details.Files.TotalCount,
                IncomingRelationCount = details.IncomingRelations.Count,
                OutgoingRelationCount = details.OutgoingRelations.Count,
                UsageCount = usage.UsageCount,
                Metadata = details.Metadata,
                Tags = details.Tags,
                Files = details.Files,
                IncomingRelations = details.IncomingRelations,
                OutgoingRelations = details.OutgoingRelations,
                Usage = usage
            };
        }

        public async Task<VaultNoteContextDto?> GetNoteContextSummaryAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var details = await _noteDetailsRead.GetNoteDetailsAsync(noteId, ct);
            if (details == null)
                return null;

            var usage = await _noteUsageRead.GetNoteUsageAsync(noteId, ct);

            return new VaultNoteContextDto
            {
                NoteID = details.NoteID,
                VaultID = details.VaultID,
                SessionID = details.SessionID,
                Title = details.Title,
                Summary = details.Summary,
                Content = details.Content,
                IsPinned = details.IsPinned,
                IsTemplate = details.IsTemplate,
                MetadataCount = details.Metadata.Count,
                TagCount = details.Tags.Count,
                FileCount = details.Files.TotalCount,
                IncomingRelationCount = details.IncomingRelations.Count,
                OutgoingRelationCount = details.OutgoingRelations.Count,
                UsageCount = usage.UsageCount,
                Metadata = details.Metadata,
                Tags = details.Tags,
                Files = details.Files,
                IncomingRelations = details.IncomingRelations,
                OutgoingRelations = details.OutgoingRelations,
                Usage = usage
            };
        }
    }
}
