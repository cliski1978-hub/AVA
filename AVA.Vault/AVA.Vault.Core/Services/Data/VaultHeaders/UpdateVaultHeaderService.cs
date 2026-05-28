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
    /// Updates an existing VaultHeader's DisplayName or Description.
    /// </summary>
    public class UpdateVaultHeaderService : ApiServiceBase<UpdateVaultHeaderRequest, UpdateVaultHeaderResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultHeaderService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultHeaderResponse DoWork(UpdateVaultHeaderRequest request)
        {
            var response = new UpdateVaultHeaderResponse();

            try
            {
                var vault = Context.Set<VaultHeader>()
                    .FirstOrDefault(v => v.ID == request.VaultId);

                if (vault == null)
                {
                    response.UserMessage = $"Vault '{request.VaultId}' not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.DisplayName))
                    vault.DisplayName = request.DisplayName;

                if (request.Description != null)
                    vault.Description = request.Description;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultHeaderService),
                    $"Updated VaultHeader [{vault.ID}] '{vault.DisplayName}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultHeader", vault.ID, "Updated");

                response.VaultId     = vault.ID;
                response.Vault       = vault;
                response.UserMessage = "Vault updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultHeaderService), "Error updating VaultHeader.", ex);
                response.UserMessage = "An error occurred while updating the vault.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultHeaderRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string VaultId { get; set; }

        [MaxLength(256)]
        public string? DisplayName { get; set; }

        public string? Description { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultId))
                yield return new ValidationResult("VaultId is required.");
        }
    }

    public class UpdateVaultHeaderResponse : CfkApiResponse
    {
        public string? VaultId { get; set; }
        public VaultHeader? Vault { get; set; }
    }

    #endregion
}
