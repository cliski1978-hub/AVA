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
    /// Creates and persists a new VaultHeaderFileRef link between a VaultHeader and VaultFileRef.
    /// </summary>
    public class CreateVaultHeaderFileRefService : ApiServiceBase<CreateVaultHeaderFileRefRequest, CreateVaultHeaderFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultHeaderFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultHeaderFileRefResponse DoWork(CreateVaultHeaderFileRefRequest request)
        {
            var response = new CreateVaultHeaderFileRefResponse();

            try
            {
                var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                if (!vaultExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultHeader [{request.VaultID}] was not found.";
                    return response;
                }

                var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                if (!fileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef [{request.FileRefID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultHeaderFileRef>().Any(f => f.ID == request.HeaderFileRefID || (f.VaultID == request.VaultID && f.FileRefID == request.FileRefID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference is already linked to this vault.";
                    return response;
                }

                var headerFileRef = new VaultHeaderFileRef
                {
                    ID = string.IsNullOrWhiteSpace(request.HeaderFileRefID) ? Guid.NewGuid().ToString() : request.HeaderFileRefID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    VaultID = request.VaultID,
                    FileRefID = request.FileRefID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultHeaderFileRef>().Add(headerFileRef);
                Context.Flush();

                response.HeaderFileRefID = headerFileRef.ID;
                response.HeaderFileRef = headerFileRef;
                response.UserMessage = "Vault header file reference link created successfully.";

                _logger.Log(nameof(CreateVaultHeaderFileRefService), $"Created VaultHeaderFileRef [{headerFileRef.ID}] Vault [{headerFileRef.VaultID}] FileRef [{headerFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultHeaderFileRef", headerFileRef.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultHeaderFileRefService), "Error creating VaultHeaderFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault header file reference link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultHeaderFileRefRequest : CfkAuthorizedApiRequest
    {
        public string? HeaderFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string VaultID { get; set; }

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
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");

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

    public class CreateVaultHeaderFileRefResponse : CfkApiResponse
    {
        public string? HeaderFileRefID { get; set; }
        public VaultHeaderFileRef? HeaderFileRef { get; set; }
    }

    #endregion
}