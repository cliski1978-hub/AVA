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
    /// Creates and persists a new VaultFileRef.
    /// This stores the file reference record only. It does not write or copy the physical file.
    /// </summary>
    public class CreateVaultFileRefService : ApiServiceBase<CreateVaultFileRefRequest, CreateVaultFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultFileRefResponse DoWork(CreateVaultFileRefRequest request)
        {
            var response = new CreateVaultFileRefResponse();

            try
            {
                var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                if (!vaultExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultHeader [{request.VaultID}] was not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.ProjectID))
                {
                    var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID && p.VaultID == request.VaultID);

                    if (!projectExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultProject [{request.ProjectID}] was not found for this vault.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.SessionID))
                {
                    var sessionExists = Context.Set<VaultSession>().Any(s => s.ID == request.SessionID);

                    if (!sessionExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultSession [{request.SessionID}] was not found.";
                        return response;
                    }
                }

                var exists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID || (f.VaultID == request.VaultID && f.ProjectID == request.ProjectID && f.SessionID == request.SessionID && f.Path.ToLower() == request.Path.ToLower()));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference already exists in this vault context.";
                    return response;
                }

                var fileRef = new VaultFileRef
                {
                    ID = string.IsNullOrWhiteSpace(request.FileRefID) ? Guid.NewGuid().ToString() : request.FileRefID,
                    Name = request.Name,
                    Path = request.Path,
                    MimeType = request.MimeType,
                    ContentHash = request.ContentHash,
                    FileSizeBytes = request.FileSizeBytes,
                    FileOrder = request.FileOrder,
                    VaultID = request.VaultID,
                    ProjectID = request.ProjectID,
                    SessionID = request.SessionID,
                    CreatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultFileRef>().Add(fileRef);
                Context.Flush();

                response.FileRefID = fileRef.ID;
                response.FileRef = fileRef;
                response.UserMessage = "Vault file reference created successfully.";

                _logger.Log(nameof(CreateVaultFileRefService), $"Created VaultFileRef [{fileRef.ID}] '{fileRef.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRef", fileRef.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultFileRefService), "Error creating VaultFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault file reference.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultFileRefRequest : CfkAuthorizedApiRequest
    {
        public string? FileRefID { get; set; }

        public long? FileSizeBytes { get; set; }

        [MaxLength(128)]
        public string? ContentHash { get; set; }

        [MaxLength(128)]
        public string? MimeType { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        [Required]
        public string Path { get; set; }

        public int FileOrder { get; set; }

        [Required]
        [MaxLength(128)]
        public string VaultID { get; set; }

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
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required.");

            if (string.IsNullOrWhiteSpace(Path))
                yield return new ValidationResult("Path is required.");

            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultFileRefResponse : CfkApiResponse
    {
        public string? FileRefID { get; set; }
        public VaultFileRef? FileRef { get; set; }
    }

    #endregion
}