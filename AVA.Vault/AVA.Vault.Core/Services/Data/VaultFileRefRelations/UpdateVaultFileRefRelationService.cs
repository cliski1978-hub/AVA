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
    /// Updates an existing VaultFileRefRelation between two VaultFileRefs.
    /// This does not update either underlying VaultFileRef.
    /// </summary>
    public class UpdateVaultFileRefRelationService : ApiServiceBase<UpdateVaultFileRefRelationRequest, UpdateVaultFileRefRelationResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultFileRefRelationService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultFileRefRelationResponse DoWork(UpdateVaultFileRefRelationRequest request)
        {
            var response = new UpdateVaultFileRefRelationResponse();

            try
            {
                var fileRefRelation = Context.Set<VaultFileRefRelation>().FirstOrDefault(r => r.ID == request.FileRefRelationID);

                if (fileRefRelation == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRefRelation '{request.FileRefRelationID}' not found.";
                    return response;
                }

                var sourceFileRefID = string.IsNullOrWhiteSpace(request.SourceFileRefID) ? fileRefRelation.SourceFileRefID : request.SourceFileRefID;
                var targetFileRefID = string.IsNullOrWhiteSpace(request.TargetFileRefID) ? fileRefRelation.TargetFileRefID : request.TargetFileRefID;
                var relationType = string.IsNullOrWhiteSpace(request.RelationType) ? fileRefRelation.RelationType : request.RelationType;

                if (!string.IsNullOrWhiteSpace(request.SourceFileRefID) && request.SourceFileRefID != fileRefRelation.SourceFileRefID)
                {
                    var sourceFileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.SourceFileRefID);

                    if (!sourceFileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"Source VaultFileRef '{request.SourceFileRefID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.TargetFileRefID) && request.TargetFileRefID != fileRefRelation.TargetFileRefID)
                {
                    var targetFileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.TargetFileRefID);

                    if (!targetFileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"Target VaultFileRef '{request.TargetFileRefID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.SourceFileRefID) && request.SourceFileRefID != fileRefRelation.SourceFileRefID) || (!string.IsNullOrWhiteSpace(request.TargetFileRefID) && request.TargetFileRefID != fileRefRelation.TargetFileRefID) || (!string.IsNullOrWhiteSpace(request.RelationType) && request.RelationType != fileRefRelation.RelationType))
                {
                    var duplicateExists = Context.Set<VaultFileRefRelation>().Any(r => r.ID != fileRefRelation.ID && r.SourceFileRefID == sourceFileRefID && r.TargetFileRefID == targetFileRefID && r.RelationType.ToLower() == relationType.ToLower());

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference relation already exists.";
                        return response;
                    }
                }

                fileRefRelation.SourceFileRefID = sourceFileRefID;
                fileRefRelation.TargetFileRefID = targetFileRefID;
                fileRefRelation.RelationType = relationType;

                if (request.Description != null)
                    fileRefRelation.Description = request.Description;

                if (request.SortOrder.HasValue)
                    fileRefRelation.SortOrder = request.SortOrder.Value;

                if (request.Weight.HasValue)
                    fileRefRelation.Weight = request.Weight.Value;

                if (request.PrimaryIdentityId != null)
                    fileRefRelation.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    fileRefRelation.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    fileRefRelation.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    fileRefRelation.IdentityList = request.IdentityList;

                fileRefRelation.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultFileRefRelationService), $"Updated VaultFileRefRelation [{fileRefRelation.ID}] SourceFileRef [{fileRefRelation.SourceFileRefID}] TargetFileRef [{fileRefRelation.TargetFileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRefRelation", fileRefRelation.ID, "Updated");

                response.FileRefRelationID = fileRefRelation.ID;
                response.FileRefRelation = fileRefRelation;
                response.UserMessage = "Vault file reference relation updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultFileRefRelationService), "Error updating VaultFileRefRelation.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault file reference relation.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultFileRefRelationRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string FileRefRelationID { get; set; }

        public string? Description { get; set; }

        [MaxLength(64)]
        public string? RelationType { get; set; }

        public int? SortOrder { get; set; }

        public float? Weight { get; set; }

        [MaxLength(128)]
        public string? SourceFileRefID { get; set; }

        [MaxLength(128)]
        public string? TargetFileRefID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FileRefRelationID))
                yield return new ValidationResult("FileRefRelationID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultFileRefRelationResponse : CfkApiResponse
    {
        public string? FileRefRelationID { get; set; }
        public VaultFileRefRelation? FileRefRelation { get; set; }
    }

    #endregion
}