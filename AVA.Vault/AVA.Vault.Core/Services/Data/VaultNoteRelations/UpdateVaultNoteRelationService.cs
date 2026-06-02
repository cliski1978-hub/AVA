using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Updates an existing VaultNoteRelation between two VaultNotes.
    /// This does not update either underlying VaultNote.
    /// </summary>
    public class UpdateVaultNoteRelationService : ApiServiceBase<UpdateVaultNoteRelationRequest, UpdateVaultNoteRelationResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultNoteRelationService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultNoteRelationResponse DoWork(UpdateVaultNoteRelationRequest request)
        {
            var response = new UpdateVaultNoteRelationResponse();

            try
            {
                var noteRelation = Context.Set<VaultNoteRelation>().FirstOrDefault(r => r.ID == request.NoteRelationID);

                if (noteRelation == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNoteRelation '{request.NoteRelationID}' not found.";
                    return response;
                }

                var sourceNoteID = string.IsNullOrWhiteSpace(request.SourceNoteID) ? noteRelation.SourceNoteID : request.SourceNoteID;
                var targetNoteID = string.IsNullOrWhiteSpace(request.TargetNoteID) ? noteRelation.TargetNoteID : request.TargetNoteID;
                var relationType = string.IsNullOrWhiteSpace(request.RelationType) ? noteRelation.RelationType : request.RelationType;

                if (!string.IsNullOrWhiteSpace(request.SourceNoteID) && request.SourceNoteID != noteRelation.SourceNoteID)
                {
                    var sourceNoteExists = Context.Set<VaultNote>().Any(n => n.ID == request.SourceNoteID);

                    if (!sourceNoteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"Source VaultNote '{request.SourceNoteID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.TargetNoteID) && request.TargetNoteID != noteRelation.TargetNoteID)
                {
                    var targetNoteExists = Context.Set<VaultNote>().Any(n => n.ID == request.TargetNoteID);

                    if (!targetNoteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"Target VaultNote '{request.TargetNoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.SourceNoteID) && request.SourceNoteID != noteRelation.SourceNoteID) || (!string.IsNullOrWhiteSpace(request.TargetNoteID) && request.TargetNoteID != noteRelation.TargetNoteID) || (!string.IsNullOrWhiteSpace(request.RelationType) && request.RelationType != noteRelation.RelationType))
                {
                    var duplicateExists = Context.Set<VaultNoteRelation>().Any(r => r.ID != noteRelation.ID && r.SourceNoteID == sourceNoteID && r.TargetNoteID == targetNoteID && r.RelationType.ToLower() == relationType.ToLower());

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This note relation already exists.";
                        return response;
                    }
                }

                noteRelation.SourceNoteID = sourceNoteID;
                noteRelation.TargetNoteID = targetNoteID;
                noteRelation.RelationType = relationType;

                if (request.Description != null)
                    noteRelation.Description = request.Description;

                if (request.SortOrder.HasValue)
                    noteRelation.SortOrder = request.SortOrder.Value;

                if (request.Weight.HasValue)
                    noteRelation.Weight = request.Weight.Value;

                if (request.PrimaryIdentityId != null)
                    noteRelation.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    noteRelation.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    noteRelation.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    noteRelation.IdentityList = request.IdentityList;

                noteRelation.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultNoteRelationService), $"Updated VaultNoteRelation [{noteRelation.ID}] SourceNote [{noteRelation.SourceNoteID}] TargetNote [{noteRelation.TargetNoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteRelation", noteRelation.ID, "Updated");

                response.NoteRelationID = noteRelation.ID;
                response.NoteRelation = noteRelation;
                response.UserMessage = "Vault note relation updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultNoteRelationService), "Error updating VaultNoteRelation.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault note relation.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultNoteRelationRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string NoteRelationID { get; set; }

        public string? Description { get; set; }

        [MaxLength(64)]
        public string? RelationType { get; set; }

        public int? SortOrder { get; set; }

        public float? Weight { get; set; }

        [MaxLength(128)]
        public string? SourceNoteID { get; set; }

        [MaxLength(128)]
        public string? TargetNoteID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(NoteRelationID))
                yield return new ValidationResult("NoteRelationID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultNoteRelationResponse : CfkApiResponse
    {
        public string? NoteRelationID { get; set; }
        public VaultNoteRelation? NoteRelation { get; set; }
    }

    #endregion
}