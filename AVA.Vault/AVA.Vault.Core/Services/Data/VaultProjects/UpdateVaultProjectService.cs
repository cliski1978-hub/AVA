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
    /// Updates an existing VaultProject.
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
                var project = Context.Set<VaultProject>().FirstOrDefault(p => p.ID == request.ProjectID);

                if (project == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultProject '{request.ProjectID}' not found.";
                    return response;
                }

                var vaultID = string.IsNullOrWhiteSpace(request.VaultID) ? project.VaultID : request.VaultID;

                if (!string.IsNullOrWhiteSpace(request.VaultID) && request.VaultID != project.VaultID)
                {
                    var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                    if (!vaultExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultHeader '{request.VaultID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    var duplicateNameExists = Context.Set<VaultProject>().Any(p => p.ID != project.ID && p.VaultID == vaultID && p.Name.ToLower() == request.Name.ToLower());

                    if (duplicateNameExists)
                    {
                        response.Code = 400;
                        response.UserMessage = $"A project named '{request.Name}' already exists in this vault.";
                        return response;
                    }

                    project.Name = request.Name;
                }

                project.VaultID = vaultID;

                if (request.Description != null)
                    project.Description = request.Description;

                if (request.IsArchived.HasValue)
                    project.IsArchived = request.IsArchived.Value;

                if (request.IsExpanded.HasValue)
                    project.IsExpanded = request.IsExpanded.Value;

                if (request.SortOrder.HasValue)
                    project.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.Status))
                    project.Status = request.Status;

                if (request.PrimaryIdentityId != null)
                    project.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    project.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    project.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    project.IdentityList = request.IdentityList;

                project.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                response.ProjectID = project.ID;
                response.Project = project;
                response.UserMessage = "Vault project updated successfully.";

                _logger.Log(nameof(UpdateVaultProjectService), $"Updated VaultProject [{project.ID}] '{project.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProject", project.ID, "Updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultProjectService), "Error updating VaultProject.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault project.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultProjectRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string ProjectID { get; set; }

        [MaxLength(128)]
        public string? VaultID { get; set; }

        [MaxLength(256)]
        public string? Name { get; set; }

        public string? Description { get; set; }

        public bool? IsArchived { get; set; }

        public bool? IsExpanded { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? Status { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("ProjectID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultProjectResponse : CfkApiResponse
    {
        public string? ProjectID { get; set; }
        public VaultProject? Project { get; set; }
    }

    #endregion
}