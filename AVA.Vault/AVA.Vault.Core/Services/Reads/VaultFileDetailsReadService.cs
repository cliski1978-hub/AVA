using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Files;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultFileDetailsReadService : IVaultFileDetailsReadService
    {
        private readonly IVaultFileRefQueryService _fileRefQuery;
        private readonly IVaultFileRefNoteQueryService _fileRefNoteQuery;
        private readonly IVaultNoteQueryService _noteQuery;
        private readonly IVaultFileRefRelationQueryService _relationQuery;
        private readonly IVaultFileUsageReadService _fileUsageRead;
        private readonly VaultLogger _logger;

        public VaultFileDetailsReadService(
            IVaultFileRefQueryService fileRefQuery,
            IVaultFileRefNoteQueryService fileRefNoteQuery,
            IVaultNoteQueryService noteQuery,
            IVaultFileRefRelationQueryService relationQuery,
            IVaultFileUsageReadService fileUsageRead,
            VaultLogger logger)
        {
            _fileRefQuery = fileRefQuery ?? throw new ArgumentNullException(nameof(fileRefQuery));
            _fileRefNoteQuery = fileRefNoteQuery ?? throw new ArgumentNullException(nameof(fileRefNoteQuery));
            _noteQuery = noteQuery ?? throw new ArgumentNullException(nameof(noteQuery));
            _relationQuery = relationQuery ?? throw new ArgumentNullException(nameof(relationQuery));
            _fileUsageRead = fileUsageRead ?? throw new ArgumentNullException(nameof(fileUsageRead));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultFileDetailsDto?> GetFileDetailsAsync(string fileRefId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fileRefId))
                throw new ArgumentException("FileRef ID is required.", nameof(fileRefId));

            var fileRef = await _fileRefQuery.GetByIdAsync(fileRefId, ct);
            if (fileRef == null)
                return null;

            var notesTask = BuildNotesResponseAsync(fileRefId, ct);
            var incomingTask = _relationQuery.GetIncomingByFileRefIdAsync(fileRefId, ct);
            var outgoingTask = _relationQuery.GetOutgoingByFileRefIdAsync(fileRefId, ct);
            var usageTask = _fileUsageRead.GetFileUsageAsync(fileRefId, ct);

            await Task.WhenAll(notesTask, incomingTask, outgoingTask, usageTask);

            return new VaultFileDetailsDto
            {
                FileRefID = fileRef.ID,
                VaultID = fileRef.VaultID,
                ProjectID = fileRef.ProjectID,
                SessionID = fileRef.SessionID,
                Name = fileRef.Name,
                Path = fileRef.Path,
                MimeType = fileRef.MimeType,
                FileSizeBytes = fileRef.FileSizeBytes,
                ContentHash = fileRef.ContentHash,
                FileOrder = fileRef.FileOrder,
                CreatedAt = fileRef.CreatedAt,
                Notes = notesTask.Result,
                IncomingRelations = incomingTask.Result
                    .OrderBy(r => r.SortOrder)
                    .Select(MapRelationDto)
                    .ToList(),
                OutgoingRelations = outgoingTask.Result
                    .OrderBy(r => r.SortOrder)
                    .Select(MapRelationDto)
                    .ToList(),
                Usage = usageTask.Result
            };
        }

        public async Task<VaultAttachedNotesResponse> GetFileNotesAsync(string fileRefId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fileRefId))
                throw new ArgumentException("FileRef ID is required.", nameof(fileRefId));

            return await BuildNotesResponseAsync(fileRefId, ct);
        }

        public async Task<VaultFileUsageDto> GetFileUsageAsync(string fileRefId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(fileRefId))
                throw new ArgumentException("FileRef ID is required.", nameof(fileRefId));

            return await _fileUsageRead.GetFileUsageAsync(fileRefId, ct);
        }

        private async Task<VaultAttachedNotesResponse> BuildNotesResponseAsync(string fileRefId, CancellationToken ct)
        {
            var links = await _fileRefNoteQuery.GetByFileRefIdAsync(fileRefId, ct);
            var noteIds = links.Select(l => l.NoteID).Distinct().ToList();
            var notes = noteIds.Count > 0
                ? (await _noteQuery.GetByIdsAsync(noteIds, ct)).ToDictionary(n => n.ID)
                : new Dictionary<string, VaultNote>();

            var dtos = new List<VaultAttachedNoteDto>();
            int requiredCount = 0, optionalCount = 0;

            foreach (var link in links.OrderBy(l => l.NoteOrder))
            {
                if (!notes.TryGetValue(link.NoteID, out var note))
                    continue;

                dtos.Add(new VaultAttachedNoteDto
                {
                    LinkID = link.ID,
                    NoteID = note.ID,
                    ParentID = fileRefId,
                    ParentType = "FileRef",
                    Title = note.Title,
                    Summary = note.Summary,
                    UsageRole = link.UsageRole,
                    Instructions = link.Instructions,
                    IsRequired = link.IsRequired,
                    SortOrder = link.NoteOrder,
                    CreatedAt = note.CreatedAt,
                    UpdatedAt = note.UpdatedAt
                });

                if (link.IsRequired)
                    requiredCount++;
                else
                    optionalCount++;
            }

            return new VaultAttachedNotesResponse
            {
                ParentID = fileRefId,
                ParentType = "FileRef",
                TotalCount = dtos.Count,
                RequiredCount = requiredCount,
                OptionalCount = optionalCount,
                Notes = dtos
            };
        }

        private static VaultFileRelationDto MapRelationDto(VaultFileRefRelation r)
        {
            return new VaultFileRelationDto
            {
                RelationID = r.ID,
                SourceFileRefID = r.SourceFileRefID,
                TargetFileRefID = r.TargetFileRefID,
                RelationType = r.RelationType,
                Description = r.Description,
                Weight = r.Weight,
                SortOrder = r.SortOrder,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            };
        }
    }
}
