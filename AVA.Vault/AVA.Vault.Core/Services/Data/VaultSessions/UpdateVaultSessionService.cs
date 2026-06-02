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
    /// Updates an existing VaultSession.
    /// </summary>
    public class UpdateVaultSessionService : ApiServiceBase<UpdateVaultSessionRequest, UpdateVaultSessionResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultSessionService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultSessionResponse DoWork(UpdateVaultSessionRequest request)
        {
            var response = new UpdateVaultSessionResponse();

            try
            {
                var session = Context.Set<VaultSession>().FirstOrDefault(s => s.ID == request.SessionID);

                if (session == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultSession '{request.SessionID}' not found.";
                    return response;
                }

                var projectID = request.ProjectID;
                var vaultID = request.VaultID;

                if (request.ProjectID != null && request.ProjectID != session.ProjectID)
                {
                    if (!string.IsNullOrWhiteSpace(request.ProjectID))
                    {
                        var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID);

                        if (!projectExists)
                        {
                            response.Code = 404;
                            response.UserMessage = $"VaultProject '{request.ProjectID}' not found.";
                            return response;
                        }
                    }
                }
                else
                {
                    projectID = session.ProjectID;
                }

                if (request.VaultID != null && request.VaultID != session.VaultID)
                {
                    if (!string.IsNullOrWhiteSpace(request.VaultID))
                    {
                        var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                        if (!vaultExists)
                        {
                            response.Code = 404;
                            response.UserMessage = $"VaultHeader '{request.VaultID}' not found.";
                            return response;
                        }
                    }
                }
                else
                {
                    vaultID = session.VaultID;
                }

                if (string.IsNullOrWhiteSpace(vaultID) && string.IsNullOrWhiteSpace(projectID))
                {
                    response.Code = 400;
                    response.UserMessage = "VaultID or ProjectID is required.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    var duplicateNameExists = Context.Set<VaultSession>().Any(s => s.ID != session.ID && ((!string.IsNullOrWhiteSpace(projectID) && s.ProjectID == projectID && s.Name.ToLower() == request.Name.ToLower()) || (!string.IsNullOrWhiteSpace(vaultID) && s.VaultID == vaultID && s.ProjectID == null && s.Name.ToLower() == request.Name.ToLower())));

                    if (duplicateNameExists)
                    {
                        response.Code = 400;
                        response.UserMessage = $"A session named '{request.Name}' already exists in this context.";
                        return response;
                    }

                    session.Name = request.Name;
                }

                session.ProjectID = projectID;
                session.VaultID = vaultID;

                if (request.AttachedModelIdsJson != null)
                    session.AttachedModelIdsJson = request.AttachedModelIdsJson;

                if (request.BroadcastGroupIdsJson != null)
                    session.BroadcastGroupIdsJson = request.BroadcastGroupIdsJson;

                if (request.CanvasJson != null)
                    session.CanvasJson = request.CanvasJson;

                if (request.DefaultModelId != null)
                    session.DefaultModelId = request.DefaultModelId;

                if (request.Description != null)
                    session.Description = request.Description;

                if (request.IsActive.HasValue)
                    session.IsActive = request.IsActive.Value;

                if (request.IsPinned.HasValue)
                    session.IsPinned = request.IsPinned.Value;

                if (request.IsTemplate.HasValue)
                    session.IsTemplate = request.IsTemplate.Value;

                if (request.LastActiveAt.HasValue)
                    session.LastActiveAt = request.LastActiveAt.Value;

                if (request.SortOrder.HasValue)
                    session.SortOrder = request.SortOrder.Value;

                if (request.SpawnCount.HasValue)
                    session.SpawnCount = request.SpawnCount.Value;

                if (request.TemplateName != null)
                    session.TemplateName = request.TemplateName;

                if (request.PrimaryIdentityId != null)
                    session.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    session.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    session.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    session.IdentityList = request.IdentityList;

                session.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                response.SessionID = session.ID;
                response.Session = session;
                response.UserMessage = "Vault session updated successfully.";

                _logger.Log(nameof(UpdateVaultSessionService), $"Updated VaultSession [{session.ID}] '{session.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSession", session.ID, "Updated");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultSessionService), "Error updating VaultSession.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault session.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultSessionRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string SessionID { get; set; }

        public string? AttachedModelIdsJson { get; set; }

        public string? BroadcastGroupIdsJson { get; set; }

        public string? CanvasJson { get; set; }

        [MaxLength(128)]
        public string? DefaultModelId { get; set; }

        public string? Description { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsPinned { get; set; }

        public bool? IsTemplate { get; set; }

        public DateTime? LastActiveAt { get; set; }

        [MaxLength(256)]
        public string? Name { get; set; }

        public int? SortOrder { get; set; }

        public int? SpawnCount { get; set; }

        [MaxLength(256)]
        public string? TemplateName { get; set; }

        [MaxLength(128)]
        public string? ProjectID { get; set; }

        [MaxLength(128)]
        public string? VaultID { get; set; }

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

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultSessionResponse : CfkApiResponse
    {
        public string? SessionID { get; set; }
        public VaultSession? Session { get; set; }
    }

    #endregion
}