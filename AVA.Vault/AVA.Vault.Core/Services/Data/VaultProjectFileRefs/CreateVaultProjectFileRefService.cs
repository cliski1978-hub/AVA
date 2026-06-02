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
    /// Creates and persists a new VaultProjectFileRef link between a VaultProject and VaultFileRef.
    /// </summary>
    public class CreateVaultProjectFileRefService : ApiServiceBase<CreateVaultProjectFileRefRequest, CreateVaultProjectFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultProjectFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultProjectFileRefResponse DoWork(CreateVaultProjectFileRefRequest request)
        {
            var response = new CreateVaultProjectFileRefResponse();

            try
            {
                var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID);

                if (!projectExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultProject [{request.ProjectID}] was not found.";
                    return response;
                }

                var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                if (!fileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef [{request.FileRefID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultProjectFileRef>().Any(f => f.ID == request.ProjectFileRefID || (f.ProjectID == request.ProjectID && f.FileRefID == request.FileRefID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference is already linked to this project.";
                    return response;
                }

                var projectFileRef = new VaultProjectFileRef
                {
                    ID = string.IsNullOrWhiteSpace(request.ProjectFileRefID) ? Guid.NewGuid().ToString() : request.ProjectFileRefID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    ProjectID = request.ProjectID,
                    FileRefID = request.FileRefID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultProjectFileRef>().Add(projectFileRef);
                Context.Flush();

                response.ProjectFileRefID = projectFileRef.ID;
                response.ProjectFileRef = projectFileRef;
                response.UserMessage = "Vault project file reference link created successfully.";

                _logger.Log(nameof(CreateVaultProjectFileRefService), $"Created VaultProjectFileRef [{projectFileRef.ID}] Project [{projectFileRef.ProjectID}] FileRef [{projectFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProjectFileRef", projectFileRef.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultProjectFileRefService), "Error creating VaultProjectFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault project file reference link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultProjectFileRefRequest : CfkAuthorizedApiRequest
    {
        public string? ProjectFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string ProjectID { get; set; }

        [Required]
        [MaxLength(128)]
        public string FileRefID { get; set; }

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

            if (string.IsNullOrWhiteSpace(FileRefID))
                yield return new ValidationResult("FileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultProjectFileRefResponse : CfkApiResponse
    {
        public string? ProjectFileRefID { get; set; }
        public VaultProjectFileRef? ProjectFileRef { get; set; }
    }

    #endregion
}