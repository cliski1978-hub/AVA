using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Retrieves all VaultNotes related to a given note via VaultNoteRelations.
    /// Returns both outgoing (source→target) and incoming (target→source) relationships.
    /// No graph traversal — direct links only. No AI-generated links.
    /// </summary>
    public class GetRelatedVaultNotesService : ApiServiceBase<GetRelatedVaultNotesRequest, GetRelatedVaultNotesResponse>
    {
        private readonly VaultLogger _logger;

        public GetRelatedVaultNotesService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override GetRelatedVaultNotesResponse DoWork(GetRelatedVaultNotesRequest request)
        {
            var response = new GetRelatedVaultNotesResponse();

            try
            {
                // Outgoing relations — this note links to others
                var outgoing = Context.Set<VaultNoteRelation>()
                    .Include(r => r.IncomingNote)
                    .Where(r => r.SourceNoteID == request.NoteID &&
                                (request.RelationType == null || r.RelationType == request.RelationType))
                    .ToList()
                    .Select(r => new RelatedNoteResult
                    {
                        Note         = r.IncomingNote,
                        LinkID       = r.ID,
                        RelationType = r.RelationType,
                        Direction    = LinkDirection.Outgoing,
                        Weight       = r.Weight,
                        Description  = r.Description
                    });

                // Incoming relations — other notes link to this one
                var incoming = Context.Set<VaultNoteRelation>()
                    .Include(r => r.OutgoingNote)
                    .Where(r => r.TargetNoteID == request.NoteID &&
                                (request.RelationType == null || r.RelationType == request.RelationType))
                    .ToList()
                    .Select(r => new RelatedNoteResult
                    {
                        Note         = r.OutgoingNote,
                        LinkID       = r.ID,
                        RelationType = r.RelationType,
                        Direction    = LinkDirection.Incoming,
                        Weight       = r.Weight,
                        Description  = r.Description
                    });

                response.Related     = outgoing.Concat(incoming).ToList();
                response.UserMessage = $"{response.Related.Count} related note(s) found.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(GetRelatedVaultNotesService), "Error retrieving related notes.", ex);
                response.UserMessage = "An error occurred while retrieving related notes.";
            }

            return response;
        }
    }

    #region Models

    public enum LinkDirection
    {
        Outgoing,
        Incoming
    }

    public class RelatedNoteResult
    {
        public VaultNote?   Note         { get; set; }
        public string       LinkID       { get; set; } = string.Empty;
        public string       RelationType { get; set; } = string.Empty;
        public LinkDirection Direction   { get; set; }
        public double       Weight       { get; set; }
        public string?      Description  { get; set; }
    }

    public class GetRelatedVaultNotesRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string NoteID  { get; set; }

        /// <summary>Optional — filter by a specific RelationType. Null returns all types.</summary>
        public string? RelationType { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID)) yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(NoteID))  yield return new ValidationResult("NoteID is required.");
            if (RelationType != null && !VaultLinkRelationType.IsValid(RelationType))
                yield return new ValidationResult($"Invalid RelationType '{RelationType}'.");
        }
    }

    public class GetRelatedVaultNotesResponse : CfkApiResponse
    {
        public List<RelatedNoteResult> Related { get; set; } = new();
    }

    #endregion
}
