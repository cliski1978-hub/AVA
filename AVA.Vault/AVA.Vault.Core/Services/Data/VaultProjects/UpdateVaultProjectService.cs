using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Logger;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;

namespace AVA.Vault.Core.Services.Data.VaultProjects
{
    /// <summary>
    /// Updates an existing VaultProject�s name or description.
    /// </summary>
    public class UpdateVaultProjectService : ApiServiceBase<UpdateVaultProjectRequest, UpdateVaultProjectResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultProjectService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultProjectResponse DoWork(UpdateVaultProjectRequest request)
        {
            var response = new UpdateVaultProjectResponse();

            try
            {
                var project = Context.Set<VaultProject>()
                    .FirstOrDefault(p => p.ID == request.ProjectID && p.VaultID == request.VaultID);

                if (project == null)
                {
                    response.UserMessage = "Vault project not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                    project.Name = request.Name;

                if (request.Description != null)
                    project.Description = request.Description;

                project.UpdatedAt = DateTime.UtcNow;
                Context.Flush();

                _logger.Log(nameof(UpdateVaultProjectService),
                    $"Updated VaultProject [{project.ID}] '{project.Name}' in Vault {project.VaultID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProject", project.ID, "Updated");

                response.ProjectID = project.ID;
                response.UserMessage = "Vault project updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultProjectService), "Error updating VaultProject.", ex);
                response.UserMessage = "An error occurred while updating the vault project.";
            }

            return response;
        }
    }

    #region Models
    public class UpdateVaultProjectRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string ProjectID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("ProjectID is required.");
        }
    }

    public class UpdateVaultProjectResponse : CfkApiResponse
    {
        public string? ProjectID { get; set; }
    }
    #endregion
}
