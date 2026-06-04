using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Dtos.Notes;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Services.Interfaces;

namespace AVA.Vault.Core.Services.Reads
{
    public sealed class VaultNoteDetailsReadService : IVaultNoteDetailsReadService
    {
        private readonly IVaultNoteQueryService _noteQuery;
        private readonly IVaultMetadataQueryService _metadataQuery;
        private readonly IVaultNoteVaultTagQueryService _noteTagQuery;
        private readonly IVaultTagQueryService _tagQuery;
        private readonly IVaultNoteFileRefQueryService _noteFileRefQuery;
        private readonly IVaultFileRefQueryService _fileRefQuery;
        private readonly IVaultNoteRelationQueryService _relationQuery;
        private readonly VaultLogger _logger;

        public VaultNoteDetailsReadService(
            IVaultNoteQueryService noteQuery,
            IVaultMetadataQueryService metadataQuery,
            IVaultNoteVaultTagQueryService noteTagQuery,
            IVaultTagQueryService tagQuery,
            IVaultNoteFileRefQueryService noteFileRefQuery,
            IVaultFileRefQueryService fileRefQuery,
            IVaultNoteRelationQueryService relationQuery,
            VaultLogger logger)
        {
            _noteQuery = noteQuery ?? throw new ArgumentNullException(nameof(noteQuery));
            _metadataQuery = metadataQuery ?? throw new ArgumentNullException(nameof(metadataQuery));
            _noteTagQuery = noteTagQuery ?? throw new ArgumentNullException(nameof(noteTagQuery));
            _tagQuery = tagQuery ?? throw new ArgumentNullException(nameof(tagQuery));
            _noteFileRefQuery = noteFileRefQuery ?? throw new ArgumentNullException(nameof(noteFileRefQuery));
            _fileRefQuery = fileRefQuery ?? throw new ArgumentNullException(nameof(fileRefQuery));
            _relationQuery = relationQuery ?? throw new ArgumentNullException(nameof(relationQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultNoteDetailsDto?> GetNoteDetailsAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var note = await _noteQuery.GetByIdAsync(noteId, ct);
            if (note == null)
                return null;

            var metadata = await _metadataQuery.GetByNoteIdAsync(noteId, ct);
            var tagLinks = await _noteTagQuery.GetByNoteIdAsync(noteId, ct);
            var tagIds = tagLinks.Select(t => t.TagID).Distinct().ToList();
            var tags = tagIds.Count > 0
                ? (await _tagQuery.GetByIdsAsync(tagIds, ct)).ToDictionary(t => t.ID)
                : new Dictionary<string, VaultTag>();
            var filesResponse = await BuildFilesResponseAsync(noteId, ct);
            var incomingRelations = await _relationQuery.GetIncomingByNoteIdAsync(noteId, ct);
            var outgoingRelations = await _relationQuery.GetOutgoingByNoteIdAsync(noteId, ct);

            return new VaultNoteDetailsDto
            {
                NoteID = note.ID,
                VaultID = note.VaultID,
                SessionID = note.SessionID,
                Title = note.Title,
                Summary = note.Summary,
                Content = note.Content,
                MetadataJson = note.MetadataJson,
                EmbeddingJson = note.EmbeddingJson,
                IsPinned = note.IsPinned,
                IsSynced = note.IsSynced,
                IsTemplate = note.IsTemplate,
                TemplateName = note.TemplateName,
                SortOrder = note.SortOrder,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt,
                Metadata = metadata
                    .OrderBy(m => m.Key)
                    .Select(MapMetadataDto)
                    .ToList(),
                Tags = MapTagDtos(tagLinks, tags).ToList(),
                Files = filesResponse,
                IncomingRelations = incomingRelations
                    .OrderBy(r => r.SortOrder)
                    .Select(MapRelationDto)
                    .ToList(),
                OutgoingRelations = outgoingRelations
                    .OrderBy(r => r.SortOrder)
                    .Select(MapRelationDto)
                    .ToList()
            };
        }

        public async Task<List<VaultNoteMetadataDto>> GetNoteMetadataAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var metadata = await _metadataQuery.GetByNoteIdAsync(noteId, ct);
            return metadata
                .OrderBy(m => m.Key)
                .Select(MapMetadataDto)
                .ToList();
        }

        public async Task<List<VaultNoteTagDto>> GetNoteTagsAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var tagLinks = await _noteTagQuery.GetByNoteIdAsync(noteId, ct);
            var tagIds = tagLinks.Select(t => t.TagID).Distinct().ToList();
            var tags = tagIds.Count > 0
                ? (await _tagQuery.GetByIdsAsync(tagIds, ct)).ToDictionary(t => t.ID)
                : new Dictionary<string, VaultTag>();

            return MapTagDtos(tagLinks, tags).ToList();
        }

        public async Task<VaultAttachedFilesResponse> GetNoteFilesAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            return await BuildFilesResponseAsync(noteId, ct);
        }

        public async Task<List<VaultNoteRelationDto>> GetNoteRelationsAsync(string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var incoming = await _relationQuery.GetIncomingByNoteIdAsync(noteId, ct);
            var outgoing = await _relationQuery.GetOutgoingByNoteIdAsync(noteId, ct);

            return incoming
                .Concat(outgoing)
                .OrderBy(r => r.SortOrder)
                .Select(MapRelationDto)
                .ToList();
        }

        private async Task<VaultAttachedFilesResponse> BuildFilesResponseAsync(string noteId, CancellationToken ct)
        {
            var links = await _noteFileRefQuery.GetByNoteIdAsync(noteId, ct);
            var fileRefIds = links.Select(l => l.FileRefID).Distinct().ToList();
            var fileRefs = fileRefIds.Count > 0
                ? (await _fileRefQuery.GetByIdsAsync(fileRefIds, ct)).ToDictionary(f => f.ID)
                : new Dictionary<string, VaultFileRef>();

            var dtos = new List<VaultAttachedFileDto>();
            int requiredCount = 0, optionalCount = 0;

            foreach (var link in links.OrderBy(l => l.SortOrder))
            {
                if (!fileRefs.TryGetValue(link.FileRefID, out var fileRef))
                    continue;

                dtos.Add(new VaultAttachedFileDto
                {
                    LinkID = link.ID,
                    FileRefID = fileRef.ID,
                    ParentID = noteId,
                    ParentType = "Note",
                    Name = fileRef.Name,
                    Path = fileRef.Path,
                    MimeType = fileRef.MimeType,
                    FileSizeBytes = fileRef.FileSizeBytes,
                    ContentHash = fileRef.ContentHash,
                    UsageRole = link.UsageRole,
                    Instructions = link.Instructions,
                    IsRequired = link.IsRequired,
                    SortOrder = link.SortOrder,
                    CreatedAt = fileRef.CreatedAt,
                    UpdatedAt = link.UpdatedAt
                });

                if (link.IsRequired)
                    requiredCount++;
                else
                    optionalCount++;
            }

            return new VaultAttachedFilesResponse
            {
                ParentID = noteId,
                ParentType = "Note",
                TotalCount = dtos.Count,
                RequiredCount = requiredCount,
                OptionalCount = optionalCount,
                Files = dtos
            };
        }

        private static VaultNoteMetadataDto MapMetadataDto(VaultMetadata m)
        {
            return new VaultNoteMetadataDto
            {
                MetadataID = m.ID,
                NoteID = m.NoteID,
                Key = m.Key,
                Value = m.Value,
                OwnerID = m.OwnerID,
                CreatedAt = m.CreatedAt,
                UpdatedAt = m.UpdatedAt
            };
        }

        private static IEnumerable<VaultNoteTagDto> MapTagDtos(
            List<VaultNoteVaultTag> tagLinks,
            Dictionary<string, VaultTag> tags)
        {
            foreach (var link in tagLinks.OrderBy(l => l.SortOrder))
            {
                if (!tags.TryGetValue(link.TagID, out var tag))
                    continue;

                yield return new VaultNoteTagDto
                {
                    LinkID = link.ID,
                    TagID = tag.ID,
                    NoteID = link.NoteID,
                    Name = tag.Name,
                    Color = tag.Color,
                    Metadata = tag.Metadata,
                    IsArchived = tag.IsArchived,
                    SortOrder = link.SortOrder,
                    CreatedAt = link.CreatedAt,
                    UpdatedAt = link.UpdatedAt
                };
            }
        }

        private static VaultNoteRelationDto MapRelationDto(VaultNoteRelation r)
        {
            return new VaultNoteRelationDto
            {
                RelationID = r.ID,
                SourceNoteID = r.SourceNoteID,
                TargetNoteID = r.TargetNoteID,
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
