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
    /// Updates the value or owner of an existing VaultMetadata record.
    /// </summary>
    public class UpdateVaultMetadataService : ApiServiceBase<UpdateVaultMetadataRequest, UpdateVaultMetadataResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultMetadataService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultMetadataResponse DoWork(UpdateVaultMetadataRequest request)
        {
            var response = new UpdateVaultMetadataResponse();

            try
            {
                var meta = Context.Set<VaultMetadata>()
                    .FirstOrDefault(m => m.ID == request.MetadataID);

                if (meta == null)
                {
                    response.UserMessage = "Metadata entry not found.";
                    return response;
                }

                if (request.Value != null)
                    meta.Value = request.Value;

                if (request.OwnerID != null)
                    meta.OwnerID = request.OwnerID;

                meta.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultMetadataService),
                    $"Updated metadata '{meta.Key}' for note {meta.NoteID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultMetadata", meta.ID, "Updated");

                response.MetadataID = meta.ID;
                response.UserMessage = "Metadata updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultMetadataService), "Error updating VaultMetadata.", ex);
                response.UserMessage = "An error occurred while updating the metadata entry.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultMetadataRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string MetadataID { get; set; }
        public string? Value { get; set; }
        public string? OwnerID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(MetadataID))
                yield return new ValidationResult("MetadataID is required.");
        }
    }

    public class UpdateVaultMetadataResponse : CfkApiResponse
    {
        public string? MetadataID { get; set; }
    }

    #endregion
}
