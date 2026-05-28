using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Creates and persists a user-created VaultNoteRelation between two VaultNotes.
    /// RelationType must be one of the values in VaultLinkRelationType.
    /// No AI-generated or graph-traversal links — user-created only.
    /// </summary>
    public class CreateVaultLinkService : ApiServiceBase<CreateVaultLinkRequest, CreateVaultLinkResponse>
    {
        private readonly VaultLogger _logger;
        private readonly IVaultIdService _ids;

        public CreateVaultLinkService(IDbContext context, VaultLogger logger, IVaultIdService ids) : base(context)
        {
            _logger = logger;
            _ids    = ids;
        }

        protected override CreateVaultLinkResponse DoWork(CreateVaultLinkRequest request)
        {
            var response = new CreateVaultLinkResponse();

            try
            {
                if (!VaultLinkRelationType.IsValid(request.RelationType))
                {
                    response.UserMessage = $"Invalid RelationType '{request.RelationType}'. Must be one of: {string.Join(", ", VaultLinkRelationType.All)}.";
                    return response;
                }

                var existing = Context.Set<VaultNoteRelation>()
                    .FirstOrDefault(r =>
                        r.SourceNoteID == request.SourceNoteID &&
                        r.TargetNoteID == request.TargetNoteID &&
                        r.RelationType == request.RelationType);

                if (existing != null)
                {
                    response.UserMessage = "A link of this type already exists between the specified notes.";
                    response.LinkID      = existing.ID;
                    response.Link        = existing;
                    return response;
                }

                var relation = new VaultNoteRelation
                {
                    ID           = _ids.NewId(),
                    SourceNoteID = request.SourceNoteID,
                    TargetNoteID = request.TargetNoteID,
                    RelationType = request.RelationType,
                    Weight       = (float)request.Weight,
                    Description  = request.Description ?? string.Empty,
                    CreatedAt    = DateTime.UtcNow,
                    UpdatedAt    = DateTime.UtcNow
                };

                Context.Set<VaultNoteRelation>().Add(relation);
                Context.Flush();

                _logger.Log(nameof(CreateVaultLinkService),
                    $"Created VaultNoteRelation: {relation.SourceNoteID} →[{relation.RelationType}]→ {relation.TargetNoteID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteRelation", relation.ID, "Created");

                response.LinkID      = relation.ID;
                response.Link        = relation;
                response.UserMessage = "Vault link created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultLinkService), "Error creating VaultLink.", ex);
                response.UserMessage = "An error occurred while creating the VaultLink.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultLinkRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string SourceNoteID { get; set; }
        [Required] public string TargetNoteID { get; set; }

        /// <summary>Must be a value from VaultLinkRelationType.</summary>
        [Required, MaxLength(64)]
        public string RelationType { get; set; }

        public double Weight { get; set; } = 1.0;
        public string? Description { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(SourceNoteID) || string.IsNullOrWhiteSpace(TargetNoteID))
                yield return new ValidationResult("SourceNoteID and TargetNoteID are required.");
            if (SourceNoteID == TargetNoteID)
                yield return new ValidationResult("SourceNoteID and TargetNoteID cannot be the same.");
        }
    }

    public class CreateVaultLinkResponse : CfkApiResponse
    {
        public string?    LinkID { get; set; }
        public VaultNoteRelation? Link   { get; set; }
    }

    #endregion
}
