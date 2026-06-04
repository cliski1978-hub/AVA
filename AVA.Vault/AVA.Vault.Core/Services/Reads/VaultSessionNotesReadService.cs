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
    public sealed class VaultSessionNotesReadService : IVaultSessionNotesReadService
    {
        private readonly IVaultSessionNoteQueryService _sessionNoteQuery;
        private readonly IVaultNoteQueryService _noteQuery;
        private readonly VaultLogger _logger;

        public VaultSessionNotesReadService(
            IVaultSessionNoteQueryService sessionNoteQuery,
            IVaultNoteQueryService noteQuery,
            VaultLogger logger)
        {
            _sessionNoteQuery = sessionNoteQuery ?? throw new ArgumentNullException(nameof(sessionNoteQuery));
            _noteQuery = noteQuery ?? throw new ArgumentNullException(nameof(noteQuery));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<VaultAttachedNotesResponse> GetNotesForSessionAsync(string sessionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID is required.", nameof(sessionId));

            var links = await _sessionNoteQuery.GetBySessionIdAsync(sessionId, ct);
            return await BuildResponseAsync(links, sessionId, "Session", null, ct);
        }

        public async Task<VaultAttachedNotesResponse> GetRequiredNotesForSessionAsync(string sessionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID is required.", nameof(sessionId));

            var links = await _sessionNoteQuery.GetBySessionIdAsync(sessionId, ct);
            return await BuildResponseAsync(links.Where(l => l.IsRequired), sessionId, "Session", null, ct);
        }

        public async Task<VaultAttachedNotesResponse> GetOptionalNotesForSessionAsync(string sessionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID is required.", nameof(sessionId));

            var links = await _sessionNoteQuery.GetBySessionIdAsync(sessionId, ct);
            return await BuildResponseAsync(links.Where(l => !l.IsRequired), sessionId, "Session", null, ct);
        }

        public async Task<VaultAttachedNoteDto?> GetNoteForSessionAsync(string sessionId, string noteId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID is required.", nameof(sessionId));
            if (string.IsNullOrWhiteSpace(noteId))
                throw new ArgumentException("Note ID is required.", nameof(noteId));

            var links = await _sessionNoteQuery.GetBySessionIdAsync(sessionId, ct);
            var link = links.FirstOrDefault(l => l.NoteID == noteId);
            if (link == null)
                return null;

            var note = await _noteQuery.GetByIdAsync(noteId, ct);
            if (note == null)
                return null;

            return MapToDto(link, note, sessionId, "Session");
        }

        public async Task<int> CountNotesForSessionAsync(string sessionId, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(sessionId))
                throw new ArgumentException("Session ID is required.", nameof(sessionId));

            return await _sessionNoteQuery.CountBySessionIdAsync(sessionId, ct);
        }

        private async Task<VaultAttachedNotesResponse> BuildResponseAsync(
            IEnumerable<VaultSessionNote> links,
            string parentId,
            string parentType,
            bool? isRequiredFilter,
            CancellationToken ct)
        {
            var linkList = links.ToList();
            var noteIds = linkList.Select(l => l.NoteID).Distinct().ToList();
            var notes = noteIds.Count > 0
                ? (await _noteQuery.GetByIdsAsync(noteIds, ct)).ToDictionary(n => n.ID)
                : new Dictionary<string, VaultNote>();

            var dtos = new List<VaultAttachedNoteDto>();
            int requiredCount = 0, optionalCount = 0;

            foreach (var link in linkList.OrderBy(l => l.SortOrder))
            {
                if (!notes.TryGetValue(link.NoteID, out var note))
                    continue;

                if (isRequiredFilter.HasValue && link.IsRequired != isRequiredFilter.Value)
                    continue;

                dtos.Add(MapToDto(link, note, parentId, parentType));

                if (link.IsRequired)
                    requiredCount++;
                else
                    optionalCount++;
            }

            return new VaultAttachedNotesResponse
            {
                ParentID = parentId,
                ParentType = parentType,
                TotalCount = dtos.Count,
                RequiredCount = requiredCount,
                OptionalCount = optionalCount,
                Notes = dtos
            };
        }

        private static VaultAttachedNoteDto MapToDto(
            VaultSessionNote link,
            VaultNote note,
            string parentId,
            string parentType)
        {
            return new VaultAttachedNoteDto
            {
                LinkID = link.ID,
                NoteID = note.ID,
                ParentID = parentId,
                ParentType = parentType,
                Title = note.Title,
                Summary = note.Summary,
                ContentPreview = TruncateContent(note.Content),
                UsageRole = link.UsageRole,
                Instructions = link.Instructions,
                IsRequired = link.IsRequired,
                IsPinned = note.IsPinned,
                IsTemplate = note.IsTemplate,
                SortOrder = link.SortOrder,
                CreatedAt = note.CreatedAt,
                UpdatedAt = note.UpdatedAt
            };
        }

        private static string? TruncateContent(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return null;

            return content.Length > 250 ? content[..250] + "..." : content;
        }
    }
}
