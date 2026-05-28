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
    /// Creates and persists a new VaultProject inside a given Vault.
    /// </summary>
    public class CreateVaultProjectService : ApiServiceBase<CreateVaultProjectRequest, CreateVaultProjectResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultProjectService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultProjectResponse DoWork(CreateVaultProjectRequest request)
        {
            var response = new CreateVaultProjectResponse();

            try
            {
                var exists = Context.Set<VaultProject>()
                    .Any(p => p.VaultID == request.VaultID &&
                              p.Name.ToLower() == request.Name.ToLower());

                if (exists)
                {
                    response.UserMessage = $"A project named '{request.Name}' already exists in this vault.";
                    return response;
                }

                var project = new VaultProject
                {
                    ID = string.IsNullOrWhiteSpace(request.ProjectID)
                        ? Guid.NewGuid().ToString()
                        : request.ProjectID,
                    VaultID = request.VaultID,
                    Name = request.Name,
                    Description = request.Description,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Context.Set<VaultProject>().Add(project);
                Context.Flush();

                response.ProjectID   = project.ID;
                response.UserMessage = "Vault project created successfully.";

                _logger.Log(nameof(CreateVaultProjectService),
                    $"Created VaultProject [{project.ID}] '{project.Name}' in Vault {project.VaultID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProject", project.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultProjectService), "Error creating VaultProject.", ex);
                response.UserMessage = "An error occurred while creating the vault project.";
            }

            return response;
        }
    }

    #region Models
    public class CreateVaultProjectRequest : CfkAuthorizedApiRequest
    {
        public string? ProjectID { get; set; }
        [Required] public string VaultID { get; set; }
        [Required, MaxLength(256)] public string Name { get; set; }
        public string? Description { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required.");
        }
    }

    public class CreateVaultProjectResponse : CfkApiResponse
    {
        public string? ProjectID { get; set; }
    }
    #endregion
}
