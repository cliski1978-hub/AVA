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
    /// Creates and persists a new VaultHeader (Vault root definition).
    /// </summary>
    public class CreateVaultHeaderService : ApiServiceBase<CreateVaultHeaderRequest, CreateVaultHeaderResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultHeaderService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultHeaderResponse DoWork(CreateVaultHeaderRequest request)
        {
            var response = new CreateVaultHeaderResponse();

            try
            {
                var exists = Context.Set<VaultHeader>()
                    .Any(v => v.ID == request.VaultId ||
                              v.DisplayName.ToLower() == request.DisplayName.ToLower());

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = $"A vault named '{request.DisplayName}' already exists.";
                    return response;
                }

                var vault = new VaultHeader
                {
                    ID = string.IsNullOrWhiteSpace(request.VaultId)
                        ? Guid.NewGuid().ToString()
                        : request.VaultId,

                    DisplayName = request.DisplayName,
                    OwnerId = request.OwnerId,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                Context.Set<VaultHeader>().Add(vault);
                Context.Flush();

                // Set response before logging � if logging fails the created entity is still returned
                response.VaultId     = vault.ID;
                response.Vault       = vault;
                response.UserMessage = "Vault created successfully.";

                _logger.Log(nameof(CreateVaultHeaderService),
                    $"Created VaultHeader [{vault.ID}] '{vault.DisplayName}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultHeader", vault.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultHeaderService), "Error creating VaultHeader.", ex);
                response.UserMessage = "An error occurred while creating the vault.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultHeaderRequest : CfkAuthorizedApiRequest
    {
        public string? VaultId { get; set; }

        [Required]
        [MaxLength(256)]
        public string DisplayName { get; set; }

        [MaxLength(128)]
        public string? OwnerId { get; set; }

        public string? Description { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(DisplayName))
                yield return new ValidationResult("DisplayName is required.");
        }
    }

    public class CreateVaultHeaderResponse : CfkApiResponse
    {
        public string? VaultId { get; set; }
        public VaultHeader? Vault { get; set; }
    }

    #endregion
}