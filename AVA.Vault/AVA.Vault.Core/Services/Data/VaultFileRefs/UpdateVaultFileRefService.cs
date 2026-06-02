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
    /// Updates an existing VaultFileRef.
    /// This updates the file reference metadata only. It does not modify the physical file.
    /// </summary>
    public class UpdateVaultFileRefService : ApiServiceBase<UpdateVaultFileRefRequest, UpdateVaultFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultFileRefResponse DoWork(UpdateVaultFileRefRequest request)
        {
            var response = new UpdateVaultFileRefResponse();

            try
            {
                var fileRef = Context.Set<VaultFileRef>().FirstOrDefault(f => f.ID == request.FileRefID);

                if (fileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                    return response;
                }

                var vaultID = string.IsNullOrWhiteSpace(request.VaultID) ? fileRef.VaultID : request.VaultID;
                var projectID = request.ProjectID;
                var sessionID = request.SessionID;
                var path = string.IsNullOrWhiteSpace(request.Path) ? fileRef.Path : request.Path;

                if (!string.IsNullOrWhiteSpace(request.VaultID) && request.VaultID != fileRef.VaultID)
                {
                    var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                    if (!vaultExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultHeader '{request.VaultID}' not found.";
                        return response;
                    }
                }

                if (request.ProjectID != null && request.ProjectID != fileRef.ProjectID)
                {
                    if (!string.IsNullOrWhiteSpace(request.ProjectID))
                    {
                        var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID && p.VaultID == vaultID);

                        if (!projectExists)
                        {
                            response.Code = 404;
                            response.UserMessage = $"VaultProject '{request.ProjectID}' not found for this vault.";
                            return response;
                        }
                    }
                }
                else
                {
                    projectID = fileRef.ProjectID;
                }

                if (request.SessionID != null && request.SessionID != fileRef.SessionID)
                {
                    if (!string.IsNullOrWhiteSpace(request.SessionID))
                    {
                        var sessionExists = Context.Set<VaultSession>().Any(s => s.ID == request.SessionID);

                        if (!sessionExists)
                        {
                            response.Code = 404;
                            response.UserMessage = $"VaultSession '{request.SessionID}' not found.";
                            return response;
                        }
                    }
                }
                else
                {
                    sessionID = fileRef.SessionID;
                }

                if ((!string.IsNullOrWhiteSpace(request.VaultID) && request.VaultID != fileRef.VaultID) || request.ProjectID != null || request.SessionID != null || (!string.IsNullOrWhiteSpace(request.Path) && request.Path != fileRef.Path))
                {
                    var duplicateExists = Context.Set<VaultFileRef>().Any(f => f.ID != fileRef.ID && f.VaultID == vaultID && f.ProjectID == projectID && f.SessionID == sessionID && f.Path.ToLower() == path.ToLower());

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference already exists in this vault context.";
                        return response;
                    }
                }

                fileRef.VaultID = vaultID;
                fileRef.ProjectID = projectID;
                fileRef.SessionID = sessionID;

                if (!string.IsNullOrWhiteSpace(request.Name))
                    fileRef.Name = request.Name;

                if (!string.IsNullOrWhiteSpace(request.Path))
                    fileRef.Path = request.Path;

                if (request.MimeType != null)
                    fileRef.MimeType = request.MimeType;

                if (request.ContentHash != null)
                    fileRef.ContentHash = request.ContentHash;

                if (request.FileSizeBytes.HasValue)
                    fileRef.FileSizeBytes = request.FileSizeBytes.Value;

                if (request.FileOrder.HasValue)
                    fileRef.FileOrder = request.FileOrder.Value;

                if (request.PrimaryIdentityId != null)
                    fileRef.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    fileRef.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    fileRef.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    fileRef.IdentityList = request.IdentityList;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultFileRefService), $"Updated VaultFileRef [{fileRef.ID}] '{fileRef.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRef", fileRef.ID, "Updated");

                response.FileRefID = fileRef.ID;
                response.FileRef = fileRef;
                response.UserMessage = "Vault file reference updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultFileRefService), "Error updating VaultFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault file reference.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string FileRefID { get; set; }

        public long? FileSizeBytes { get; set; }

        [MaxLength(128)]
        public string? ContentHash { get; set; }

        [MaxLength(128)]
        public string? MimeType { get; set; }

        [MaxLength(256)]
        public string? Name { get; set; }

        public string? Path { get; set; }

        public int? FileOrder { get; set; }

        [MaxLength(128)]
        public string? VaultID { get; set; }

        [MaxLength(128)]
        public string? ProjectID { get; set; }

        [MaxLength(128)]
        public string? SessionID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
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

    public class UpdateVaultFileRefResponse : CfkApiResponse
    {
        public string? FileRefID { get; set; }
        public VaultFileRef? FileRef { get; set; }
    }

    #endregion
}