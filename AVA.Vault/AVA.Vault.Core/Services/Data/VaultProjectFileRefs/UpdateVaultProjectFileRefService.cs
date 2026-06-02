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
    /// Updates an existing VaultProjectFileRef link between a VaultProject and VaultFileRef.
    /// This does not update the underlying VaultFileRef.
    /// </summary>
    public class UpdateVaultProjectFileRefService : ApiServiceBase<UpdateVaultProjectFileRefRequest, UpdateVaultProjectFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultProjectFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultProjectFileRefResponse DoWork(UpdateVaultProjectFileRefRequest request)
        {
            var response = new UpdateVaultProjectFileRefResponse();

            try
            {
                var projectFileRef = Context.Set<VaultProjectFileRef>().FirstOrDefault(f => f.ID == request.ProjectFileRefID);

                if (projectFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultProjectFileRef '{request.ProjectFileRefID}' not found.";
                    return response;
                }

                var projectID = string.IsNullOrWhiteSpace(request.ProjectID) ? projectFileRef.ProjectID : request.ProjectID;
                var fileRefID = string.IsNullOrWhiteSpace(request.FileRefID) ? projectFileRef.FileRefID : request.FileRefID;

                if (!string.IsNullOrWhiteSpace(request.ProjectID) && request.ProjectID != projectFileRef.ProjectID)
                {
                    var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID);

                    if (!projectExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultProject '{request.ProjectID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != projectFileRef.FileRefID)
                {
                    var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                    if (!fileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.ProjectID) && request.ProjectID != projectFileRef.ProjectID) || (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != projectFileRef.FileRefID))
                {
                    var duplicateExists = Context.Set<VaultProjectFileRef>().Any(f => f.ID != projectFileRef.ID && f.ProjectID == projectID && f.FileRefID == fileRefID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference is already linked to this project.";
                        return response;
                    }
                }

                projectFileRef.ProjectID = projectID;
                projectFileRef.FileRefID = fileRefID;

                if (request.Instructions != null)
                    projectFileRef.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    projectFileRef.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    projectFileRef.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    projectFileRef.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    projectFileRef.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    projectFileRef.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    projectFileRef.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    projectFileRef.IdentityList = request.IdentityList;

                projectFileRef.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultProjectFileRefService), $"Updated VaultProjectFileRef [{projectFileRef.ID}] Project [{projectFileRef.ProjectID}] FileRef [{projectFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProjectFileRef", projectFileRef.ID, "Updated");

                response.ProjectFileRefID = projectFileRef.ID;
                response.ProjectFileRef = projectFileRef;
                response.UserMessage = "Vault project file reference link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultProjectFileRefService), "Error updating VaultProjectFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault project file reference link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultProjectFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string ProjectFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? ProjectID { get; set; }

        [MaxLength(128)]
        public string? FileRefID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ProjectFileRefID))
                yield return new ValidationResult("ProjectFileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultProjectFileRefResponse : CfkApiResponse
    {
        public string? ProjectFileRefID { get; set; }
        public VaultProjectFileRef? ProjectFileRef { get; set; }
    }

    #endregion
}