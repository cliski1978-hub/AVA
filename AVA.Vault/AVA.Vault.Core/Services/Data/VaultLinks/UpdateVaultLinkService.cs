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
    /// Updates an existing VaultNoteRelation's metadata, weight, or relation type.
    /// </summary>
    public class UpdateVaultLinkService : ApiServiceBase<UpdateVaultLinkRequest, UpdateVaultLinkResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultLinkService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultLinkResponse DoWork(UpdateVaultLinkRequest request)
        {
            var response = new UpdateVaultLinkResponse();

            try
            {
                var relation = Context.Set<VaultNoteRelation>()
                    .FirstOrDefault(r => r.ID == request.LinkID);

                if (relation == null)
                {
                    response.UserMessage = "VaultNoteRelation not found.";
                    return response;
                }

                relation.RelationType = request.RelationType ?? relation.RelationType;
                relation.Weight = (float)(request.Weight ?? relation.Weight);
                relation.Description = request.Description ?? relation.Description;
                relation.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultLinkService),
                    $"Updated VaultNoteRelation [{relation.ID}] ({relation.RelationType})");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteRelation", relation.ID, "Updated");

                response.LinkID = relation.ID;
                response.UserMessage = "Vault link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultLinkService), "Error updating VaultLink.", ex);
                response.UserMessage = "An error occurred while updating the VaultLink.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultLinkRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string VaultID { get; set; }

        [Required]
        public string LinkID { get; set; }

        public string? RelationType { get; set; }

        public double? Weight { get; set; }

        public string? Description { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(LinkID))
                yield return new ValidationResult("LinkID is required.");
        }
    }

    public class UpdateVaultLinkResponse : CfkApiResponse
    {
        public string? LinkID { get; set; }
    }

    #endregion
}
