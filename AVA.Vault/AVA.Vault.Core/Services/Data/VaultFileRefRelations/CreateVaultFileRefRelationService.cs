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
    /// Creates and persists a new VaultFileRefRelation between two VaultFileRefs.
    /// </summary>
    public class CreateVaultFileRefRelationService : ApiServiceBase<CreateVaultFileRefRelationRequest, CreateVaultFileRefRelationResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultFileRefRelationService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultFileRefRelationResponse DoWork(CreateVaultFileRefRelationRequest request)
        {
            var response = new CreateVaultFileRefRelationResponse();

            try
            {
                var sourceFileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.SourceFileRefID);

                if (!sourceFileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"Source VaultFileRef [{request.SourceFileRefID}] was not found.";
                    return response;
                }

                var targetFileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.TargetFileRefID);

                if (!targetFileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"Target VaultFileRef [{request.TargetFileRefID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultFileRefRelation>().Any(r => r.ID == request.FileRefRelationID || (r.SourceFileRefID == request.SourceFileRefID && r.TargetFileRefID == request.TargetFileRefID && r.RelationType.ToLower() == request.RelationType.ToLower()));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference relation already exists.";
                    return response;
                }

                var fileRefRelation = new VaultFileRefRelation
                {
                    ID = string.IsNullOrWhiteSpace(request.FileRefRelationID) ? Guid.NewGuid().ToString() : request.FileRefRelationID,
                    Description = request.Description,
                    RelationType = request.RelationType,
                    SortOrder = request.SortOrder,
                    Weight = request.Weight,
                    SourceFileRefID = request.SourceFileRefID,
                    TargetFileRefID = request.TargetFileRefID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultFileRefRelation>().Add(fileRefRelation);
                Context.Flush();

                response.FileRefRelationID = fileRefRelation.ID;
                response.FileRefRelation = fileRefRelation;
                response.UserMessage = "Vault file reference relation created successfully.";

                _logger.Log(nameof(CreateVaultFileRefRelationService), $"Created VaultFileRefRelation [{fileRefRelation.ID}] SourceFileRef [{fileRefRelation.SourceFileRefID}] TargetFileRef [{fileRefRelation.TargetFileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRefRelation", fileRefRelation.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultFileRefRelationService), "Error creating VaultFileRefRelation.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault file reference relation.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultFileRefRelationRequest : CfkAuthorizedApiRequest
    {
        public string? FileRefRelationID { get; set; }

        public string? Description { get; set; }

        [Required]
        [MaxLength(64)]
        public string RelationType { get; set; }

        public int SortOrder { get; set; }

        public float Weight { get; set; }

        [Required]
        [MaxLength(128)]
        public string SourceFileRefID { get; set; }

        [Required]
        [MaxLength(128)]
        public string TargetFileRefID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(RelationType))
                yield return new ValidationResult("RelationType is required.");

            if (string.IsNullOrWhiteSpace(SourceFileRefID))
                yield return new ValidationResult("SourceFileRefID is required.");

            if (string.IsNullOrWhiteSpace(TargetFileRefID))
                yield return new ValidationResult("TargetFileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultFileRefRelationResponse : CfkApiResponse
    {
        public string? FileRefRelationID { get; set; }
        public VaultFileRefRelation? FileRefRelation { get; set; }
    }

    #endregion
}