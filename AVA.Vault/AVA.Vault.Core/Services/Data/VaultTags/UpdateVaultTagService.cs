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
    /// Updates an existing VaultTag�s name, color, or description.
    /// </summary>
    public class UpdateVaultTagService : ApiServiceBase<UpdateVaultTagRequest, UpdateVaultTagResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultTagService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultTagResponse DoWork(UpdateVaultTagRequest request)
        {
            var response = new UpdateVaultTagResponse();

            try
            {
                var tag = Context.Set<VaultTag>()
                    .FirstOrDefault(t => t.ID == request.TagID && t.ProjectID == request.VaultID);

                if (tag == null)
                {
                    response.UserMessage = "Vault tag not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                    tag.Name = request.Name;

                if (request.Color != null)
                    tag.Color = request.Color;

                tag.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultTagService),
                    $"Updated VaultTag [{tag.ID}] '{tag.Name}' in Vault={tag.ProjectID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultTag", tag.ID, "Updated");

                response.TagID = tag.ID;
                response.UserMessage = "Vault tag updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultTagService), "Error updating VaultTag.", ex);
                response.UserMessage = "An error occurred while updating the VaultTag.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultTagRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string TagID { get; set; }
        public string? Name { get; set; }
        public string? Color { get; set; }
        public string? Description { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(TagID))
                yield return new ValidationResult("TagID is required.");
        }
    }

    public class UpdateVaultTagResponse : CfkApiResponse
    {
        public string? TagID { get; set; }
    }

    #endregion
}
