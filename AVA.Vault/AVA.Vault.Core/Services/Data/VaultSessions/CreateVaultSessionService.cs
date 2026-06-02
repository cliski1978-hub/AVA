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
    /// Creates and persists a new VaultSession.
    /// </summary>
    public class CreateVaultSessionService : ApiServiceBase<CreateVaultSessionRequest, CreateVaultSessionResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultSessionService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultSessionResponse DoWork(CreateVaultSessionRequest request)
        {
            var response = new CreateVaultSessionResponse();

            try
            {
                if (string.IsNullOrWhiteSpace(request.VaultID) && string.IsNullOrWhiteSpace(request.ProjectID))
                {
                    response.Code = 400;
                    response.UserMessage = "VaultID or ProjectID is required.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.VaultID))
                {
                    var vaultExists = Context.Set<VaultHeader>().Any(v => v.ID == request.VaultID);

                    if (!vaultExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultHeader [{request.VaultID}] was not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.ProjectID))
                {
                    var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID);

                    if (!projectExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultProject [{request.ProjectID}] was not found.";
                        return response;
                    }
                }

                var exists = Context.Set<VaultSession>().Any(s => s.ID == request.SessionID || ((!string.IsNullOrWhiteSpace(request.ProjectID) && s.ProjectID == request.ProjectID && s.Name.ToLower() == request.Name.ToLower()) || (!string.IsNullOrWhiteSpace(request.VaultID) && s.VaultID == request.VaultID && s.ProjectID == null && s.Name.ToLower() == request.Name.ToLower())));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = $"A session named '{request.Name}' already exists in this context.";
                    return response;
                }

                var session = new VaultSession
                {
                    ID = string.IsNullOrWhiteSpace(request.SessionID) ? Guid.NewGuid().ToString() : request.SessionID,
                    AttachedModelIdsJson = request.AttachedModelIdsJson,
                    BroadcastGroupIdsJson = request.BroadcastGroupIdsJson,
                    CanvasJson = request.CanvasJson,
                    DefaultModelId = request.DefaultModelId,
                    Description = request.Description,
                    IsActive = request.IsActive,
                    IsPinned = request.IsPinned,
                    IsTemplate = request.IsTemplate,
                    LastActiveAt = request.LastActiveAt,
                    Name = request.Name,
                    SortOrder = request.SortOrder,
                    SpawnCount = request.SpawnCount,
                    TemplateName = request.TemplateName,
                    ProjectID = request.ProjectID,
                    VaultID = request.VaultID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultSession>().Add(session);
                Context.Flush();

                response.SessionID = session.ID;
                response.Session = session;
                response.UserMessage = "Vault session created successfully.";

                _logger.Log(nameof(CreateVaultSessionService), $"Created VaultSession [{session.ID}] '{session.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSession", session.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultSessionService), "Error creating VaultSession.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault session.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultSessionRequest : CfkAuthorizedApiRequest
    {
        public string? SessionID { get; set; }

        public string? AttachedModelIdsJson { get; set; }

        public string? BroadcastGroupIdsJson { get; set; }

        public string? CanvasJson { get; set; }

        [MaxLength(128)]
        public string? DefaultModelId { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public bool IsPinned { get; set; }

        public bool IsTemplate { get; set; }

        public DateTime? LastActiveAt { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        public int SortOrder { get; set; }

        public int SpawnCount { get; set; }

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
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required.");

            if (string.IsNullOrWhiteSpace(VaultID) && string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("VaultID or ProjectID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultSessionResponse : CfkApiResponse
    {
        public string? SessionID { get; set; }
        public VaultSession? Session { get; set; }
    }

    #endregion
}