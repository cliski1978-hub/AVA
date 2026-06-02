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
    /// Creates and persists a new VaultProject under a VaultHeader.
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
                var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                if (!vaultExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultHeader [{request.VaultID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID || (p.VaultID == request.VaultID && p.Name.ToLower() == request.Name.ToLower()));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = $"A project named '{request.Name}' already exists in this vault.";
                    return response;
                }

                var project = new VaultProject
                {
                    ID = string.IsNullOrWhiteSpace(request.ProjectID) ? Guid.NewGuid().ToString() : request.ProjectID,
                    VaultID = request.VaultID,
                    Name = request.Name,
                    Description = request.Description,
                    IsArchived = request.IsArchived,
                    IsExpanded = request.IsExpanded,
                    SortOrder = request.SortOrder,
                    Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultProject>().Add(project);
                Context.Flush();

                response.ProjectID = project.ID;
                response.Project = project;
                response.UserMessage = "Vault project created successfully.";

                _logger.Log(nameof(CreateVaultProjectService), $"Created VaultProject [{project.ID}] '{project.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProject", project.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultProjectService), "Error creating VaultProject.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault project.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultProjectRequest : CfkAuthorizedApiRequest
    {
        public string? ProjectID { get; set; }

        [Required]
        [MaxLength(128)]
        public string VaultID { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        public string? Description { get; set; }

        public bool IsArchived { get; set; }

        public bool IsExpanded { get; set; }

        public int SortOrder { get; set; }

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
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");

            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultProjectResponse : CfkApiResponse
    {
        public string? ProjectID { get; set; }
        public VaultProject? Project { get; set; }
    }

    #endregion
}