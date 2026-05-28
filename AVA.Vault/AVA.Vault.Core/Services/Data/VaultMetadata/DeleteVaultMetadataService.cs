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
    /// Deletes an existing VaultMetadata record.
    /// </summary>
    public class DeleteVaultMetadataService : ApiServiceBase<DeleteVaultMetadataRequest, DeleteVaultMetadataResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultMetadataService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultMetadataResponse DoWork(DeleteVaultMetadataRequest request)
        {
            var response = new DeleteVaultMetadataResponse();

            try
            {
                var meta = Context.Set<VaultMetadata>()
                    .FirstOrDefault(m => m.ID == request.MetadataID);

                if (meta == null)
                {
                    response.UserMessage = "Metadata entry not found.";
                    response.Deleted = false;
                    return response;
                }

                Context.Set<VaultMetadata>().Remove(meta);
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultMetadata", meta.ID, "Deleted");
                Context.Flush();

                _logger.Log(nameof(DeleteVaultMetadataService),
                    $"Deleted metadata '{meta.Key}' from note {meta.NoteID}");

                response.Deleted = true;
                response.UserMessage = "Metadata deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultMetadataService), "Error deleting VaultMetadata.", ex);
                response.UserMessage = "An error occurred while deleting the metadata entry.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultMetadataRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string MetadataID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(MetadataID))
                yield return new ValidationResult("MetadataID is required.");
        }
    }

    public class DeleteVaultMetadataResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}
