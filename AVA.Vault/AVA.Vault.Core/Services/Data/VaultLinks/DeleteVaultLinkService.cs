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
    /// Deletes an existing VaultNoteRelation by ID and Vault scope.
    /// </summary>
    public class DeleteVaultLinkService : ApiServiceBase<DeleteVaultLinkRequest, DeleteVaultLinkResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultLinkService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultLinkResponse DoWork(DeleteVaultLinkRequest request)
        {
            var response = new DeleteVaultLinkResponse();

            try
            {
                var relation = Context.Set<VaultNoteRelation>()
                    .FirstOrDefault(r => r.ID == request.LinkID);

                if (relation == null)
                {
                    response.UserMessage = "VaultNoteRelation not found.";
                    response.Deleted = false;
                    return response;
                }

                Context.Set<VaultNoteRelation>().Remove(relation);
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNoteRelation", relation.ID, "Deleted");
                Context.Flush();

                _logger.Log(nameof(DeleteVaultLinkService),
                    $"Deleted VaultNoteRelation [{relation.ID}] ({relation.RelationType})");

                response.Deleted = true;
                response.UserMessage = "Vault link deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultLinkService), "Error deleting VaultLink.", ex);
                response.UserMessage = "An error occurred while deleting the VaultLink.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultLinkRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string VaultID { get; set; }

        [Required]
        public string LinkID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(LinkID))
                yield return new ValidationResult("LinkID is required.");
        }
    }

    public class DeleteVaultLinkResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}
