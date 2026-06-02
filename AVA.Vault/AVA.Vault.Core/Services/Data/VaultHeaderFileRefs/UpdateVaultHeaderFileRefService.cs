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
    /// Updates an existing VaultHeaderFileRef link between a VaultHeader and VaultFileRef.
    /// This does not update the underlying VaultFileRef.
    /// </summary>
    public class UpdateVaultHeaderFileRefService : ApiServiceBase<UpdateVaultHeaderFileRefRequest, UpdateVaultHeaderFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultHeaderFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultHeaderFileRefResponse DoWork(UpdateVaultHeaderFileRefRequest request)
        {
            var response = new UpdateVaultHeaderFileRefResponse();

            try
            {
                var headerFileRef = Context.Set<VaultHeaderFileRef>().FirstOrDefault(f => f.ID == request.HeaderFileRefID);

                if (headerFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultHeaderFileRef '{request.HeaderFileRefID}' not found.";
                    return response;
                }

                var vaultID = string.IsNullOrWhiteSpace(request.VaultID) ? headerFileRef.VaultID : request.VaultID;
                var fileRefID = string.IsNullOrWhiteSpace(request.FileRefID) ? headerFileRef.FileRefID : request.FileRefID;

                if (!string.IsNullOrWhiteSpace(request.VaultID) && request.VaultID != headerFileRef.VaultID)
                {
                    var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                    if (!vaultExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultHeader '{request.VaultID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != headerFileRef.FileRefID)
                {
                    var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                    if (!fileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.VaultID) && request.VaultID != headerFileRef.VaultID) || (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != headerFileRef.FileRefID))
                {
                    var duplicateExists = Context.Set<VaultHeaderFileRef>().Any(f => f.ID != headerFileRef.ID && f.VaultID == vaultID && f.FileRefID == fileRefID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference is already linked to this vault.";
                        return response;
                    }
                }

                headerFileRef.VaultID = vaultID;
                headerFileRef.FileRefID = fileRefID;

                if (request.Instructions != null)
                    headerFileRef.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    headerFileRef.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    headerFileRef.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    headerFileRef.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    headerFileRef.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    headerFileRef.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    headerFileRef.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    headerFileRef.IdentityList = request.IdentityList;

                headerFileRef.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultHeaderFileRefService), $"Updated VaultHeaderFileRef [{headerFileRef.ID}] Vault [{headerFileRef.VaultID}] FileRef [{headerFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultHeaderFileRef", headerFileRef.ID, "Updated");

                response.HeaderFileRefID = headerFileRef.ID;
                response.HeaderFileRef = headerFileRef;
                response.UserMessage = "Vault header file reference link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultHeaderFileRefService), "Error updating VaultHeaderFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault header file reference link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultHeaderFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string HeaderFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? VaultID { get; set; }

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
            if (string.IsNullOrWhiteSpace(HeaderFileRefID))
                yield return new ValidationResult("HeaderFileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultHeaderFileRefResponse : CfkApiResponse
    {
        public string? HeaderFileRefID { get; set; }
        public VaultHeaderFileRef? HeaderFileRef { get; set; }
    }

    #endregion
}