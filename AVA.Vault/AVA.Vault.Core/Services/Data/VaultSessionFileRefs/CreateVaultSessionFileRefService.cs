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
    /// Creates and persists a new VaultSessionFileRef link between a VaultSession and VaultFileRef.
    /// </summary>
    public class CreateVaultSessionFileRefService : ApiServiceBase<CreateVaultSessionFileRefRequest, CreateVaultSessionFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultSessionFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultSessionFileRefResponse DoWork(CreateVaultSessionFileRefRequest request)
        {
            var response = new CreateVaultSessionFileRefResponse();

            try
            {
                var sessionExists = Context.Set<VaultSession>().Any(s => s.ID == request.SessionID);

                if (!sessionExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultSession [{request.SessionID}] was not found.";
                    return response;
                }

                var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                if (!fileRefExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultFileRef [{request.FileRefID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultSessionFileRef>().Any(f => f.ID == request.SessionFileRefID || (f.SessionID == request.SessionID && f.FileRefID == request.FileRefID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This file reference is already linked to this session.";
                    return response;
                }

                var sessionFileRef = new VaultSessionFileRef
                {
                    ID = string.IsNullOrWhiteSpace(request.SessionFileRefID) ? Guid.NewGuid().ToString() : request.SessionFileRefID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    SortOrder = request.SortOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    SessionID = request.SessionID,
                    FileRefID = request.FileRefID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultSessionFileRef>().Add(sessionFileRef);
                Context.Flush();

                response.SessionFileRefID = sessionFileRef.ID;
                response.SessionFileRef = sessionFileRef;
                response.UserMessage = "Vault session file reference link created successfully.";

                _logger.Log(nameof(CreateVaultSessionFileRefService), $"Created VaultSessionFileRef [{sessionFileRef.ID}] Session [{sessionFileRef.SessionID}] FileRef [{sessionFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSessionFileRef", sessionFileRef.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultSessionFileRefService), "Error creating VaultSessionFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault session file reference link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultSessionFileRefRequest : CfkAuthorizedApiRequest
    {
        public string? SessionFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string SessionID { get; set; }

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
            if (string.IsNullOrWhiteSpace(SessionID))
                yield return new ValidationResult("SessionID is required.");

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

    public class CreateVaultSessionFileRefResponse : CfkApiResponse
    {
        public string? SessionFileRefID { get; set; }
        public VaultSessionFileRef? SessionFileRef { get; set; }
    }

    #endregion
}