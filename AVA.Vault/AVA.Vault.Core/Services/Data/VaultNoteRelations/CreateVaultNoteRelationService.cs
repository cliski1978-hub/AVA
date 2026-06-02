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
    /// Creates and persists a new VaultNoteRelation between two VaultNotes.
    /// </summary>
    public class CreateVaultNoteRelationService : ApiServiceBase<CreateVaultNoteRelationRequest, CreateVaultNoteRelationResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultNoteRelationService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultNoteRelationResponse DoWork(CreateVaultNoteRelationRequest request)
        {
            var response = new CreateVaultNoteRelationResponse();

            try
            {
                var sourceNoteExists = Context.Set<VaultNote>().Any(n => n.ID == request.SourceNoteID);

                if (!sourceNoteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"Source VaultNote [{request.SourceNoteID}] was not found.";
                    return response;
                }

                var targetNoteExists = Context.Set<VaultNote>().Any(n => n.ID == request.TargetNoteID);

                if (!targetNoteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"Target VaultNote [{request.TargetNoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultNoteRelation>().Any(r => r.ID == request.NoteRelationID || (r.SourceNoteID == request.SourceNoteID && r.TargetNoteID == request.TargetNoteID && r.RelationType.ToLower() == request.RelationType.ToLower()));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This note relation already exists.";
                    return response;
                }

                var noteRelation = new VaultNoteRelation
                {
                    ID = string.IsNullOrWhiteSpace(request.NoteRelationID) ? Guid.NewGuid().ToString() : request.NoteRelationID,
                    Description = request.Description,
                    RelationType = request.RelationType,
                    SortOrder = request.SortOrder,
                    Weight = request.Weight,
                    SourceNoteID = request.SourceNoteID,
                    TargetNoteID = request.TargetNoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultNoteRelation>().Add(noteRelation);
                Context.Flush();

                response.NoteRelationID = noteRelation.ID;
                response.NoteRelation = noteRelation;
                response.UserMessage = "Vault note relation created successfully.";

                _logger.Log(nameof(CreateVaultNoteRelationService), $"Created VaultNoteRelation [{noteRelation.ID}] SourceNote [{noteRelation.SourceNoteID}] TargetNote [{noteRelation.TargetNoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteRelation", noteRelation.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultNoteRelationService), "Error creating VaultNoteRelation.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault note relation.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultNoteRelationRequest : CfkAuthorizedApiRequest
    {
        public string? NoteRelationID { get; set; }

        public string? Description { get; set; }

        [Required]
        [MaxLength(64)]
        public string RelationType { get; set; }

        public int SortOrder { get; set; }

        public float Weight { get; set; }

        [Required]
        [MaxLength(128)]
        public string SourceNoteID { get; set; }

        [Required]
        [MaxLength(128)]
        public string TargetNoteID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(RelationType))
                yield return new ValidationResult("RelationType is required.");

            if (string.IsNullOrWhiteSpace(SourceNoteID))
                yield return new ValidationResult("SourceNoteID is required.");

            if (string.IsNullOrWhiteSpace(TargetNoteID))
                yield return new ValidationResult("TargetNoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultNoteRelationResponse : CfkApiResponse
    {
        public string? NoteRelationID { get; set; }
        public VaultNoteRelation? NoteRelation { get; set; }
    }

    #endregion
}