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
    /// Updates an existing VaultSessionFileRef link between a VaultSession and VaultFileRef.
    /// This does not update the underlying VaultFileRef.
    /// </summary>
    public class UpdateVaultSessionFileRefService : ApiServiceBase<UpdateVaultSessionFileRefRequest, UpdateVaultSessionFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultSessionFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultSessionFileRefResponse DoWork(UpdateVaultSessionFileRefRequest request)
        {
            var response = new UpdateVaultSessionFileRefResponse();

            try
            {
                var sessionFileRef = Context.Set<VaultSessionFileRef>().FirstOrDefault(f => f.ID == request.SessionFileRefID);

                if (sessionFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultSessionFileRef '{request.SessionFileRefID}' not found.";
                    return response;
                }

                var sessionID = string.IsNullOrWhiteSpace(request.SessionID) ? sessionFileRef.SessionID : request.SessionID;
                var fileRefID = string.IsNullOrWhiteSpace(request.FileRefID) ? sessionFileRef.FileRefID : request.FileRefID;

                if (!string.IsNullOrWhiteSpace(request.SessionID) && request.SessionID != sessionFileRef.SessionID)
                {
                    var sessionExists = Context.Set<VaultSession>().Any(s => s.ID == request.SessionID);

                    if (!sessionExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultSession '{request.SessionID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != sessionFileRef.FileRefID)
                {
                    var fileRefExists = Context.Set<VaultFileRef>().Any(f => f.ID == request.FileRefID);

                    if (!fileRefExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultFileRef '{request.FileRefID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.SessionID) && request.SessionID != sessionFileRef.SessionID) || (!string.IsNullOrWhiteSpace(request.FileRefID) && request.FileRefID != sessionFileRef.FileRefID))
                {
                    var duplicateExists = Context.Set<VaultSessionFileRef>().Any(f => f.ID != sessionFileRef.ID && f.SessionID == sessionID && f.FileRefID == fileRefID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This file reference is already linked to this session.";
                        return response;
                    }
                }

                sessionFileRef.SessionID = sessionID;
                sessionFileRef.FileRefID = fileRefID;

                if (request.Instructions != null)
                    sessionFileRef.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    sessionFileRef.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    sessionFileRef.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    sessionFileRef.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    sessionFileRef.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    sessionFileRef.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    sessionFileRef.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    sessionFileRef.IdentityList = request.IdentityList;

                sessionFileRef.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultSessionFileRefService), $"Updated VaultSessionFileRef [{sessionFileRef.ID}] Session [{sessionFileRef.SessionID}] FileRef [{sessionFileRef.FileRefID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSessionFileRef", sessionFileRef.ID, "Updated");

                response.SessionFileRefID = sessionFileRef.ID;
                response.SessionFileRef = sessionFileRef;
                response.UserMessage = "Vault session file reference link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultSessionFileRefService), "Error updating VaultSessionFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault session file reference link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultSessionFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string SessionFileRefID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? SessionID { get; set; }

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
            if (string.IsNullOrWhiteSpace(SessionFileRefID))
                yield return new ValidationResult("SessionFileRefID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultSessionFileRefResponse : CfkApiResponse
    {
        public string? SessionFileRefID { get; set; }
        public VaultSessionFileRef? SessionFileRef { get; set; }
    }

    #endregion
}