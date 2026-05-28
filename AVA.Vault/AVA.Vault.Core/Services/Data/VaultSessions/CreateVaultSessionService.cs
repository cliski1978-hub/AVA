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
    /// Creates and persists a new VaultSession within a Vault or Project.
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
                var exists = Context.Set<VaultSession>()
                    .Any(s => s.ID == request.SessionId);

                if (exists)
                {
                    response.UserMessage = $"A session with ID '{request.SessionId}' already exists.";
                    return response;
                }

                var session = new VaultSession
                {
                    ID                  = string.IsNullOrWhiteSpace(request.SessionId)
                                            ? Guid.NewGuid().ToString()
                                            : request.SessionId,
                    VaultID             = request.VaultId,
                    ProjectID           = request.ProjectId,
                    Name                = request.Name,
                    CreatedAt           = DateTime.UtcNow,
                    LastActiveAt        = DateTime.UtcNow,
                    DefaultModelId      = request.DefaultModelId,
                    AttachedModelIdsJson  = request.AttachedModelIdsJson ?? "[]",
                    BroadcastGroupIdsJson = request.BroadcastGroupIdsJson ?? "[]",
                    CanvasJson          = request.CanvasJson ?? "{}"
                };

                Context.Set<VaultSession>().Add(session);
                Context.Flush();

                response.SessionId   = session.ID;
                response.Session     = session;
                response.UserMessage = "Session created successfully.";

                _logger.Log(nameof(CreateVaultSessionService),
                    $"Created VaultSession [{session.ID}] '{session.Name}' in Vault {session.VaultID}");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSession", session.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultSessionService), "Error creating VaultSession.", ex);
                response.UserMessage = "An error occurred while creating the session.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultSessionRequest : CfkAuthorizedApiRequest
    {
        public string? SessionId { get; set; }

        [Required]
        [MaxLength(128)]
        public string VaultId { get; set; }

        [MaxLength(128)]
        public string? ProjectId { get; set; }

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        public bool IsPinned { get; set; }
        public string? DefaultModelId { get; set; }
        public bool IsTemplate { get; set; }
        public string? TemplateName { get; set; }
        public string? AttachedModelIdsJson { get; set; }
        public string? BroadcastGroupIdsJson { get; set; }
        public string? CanvasJson { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultId))
                yield return new ValidationResult("VaultId is required.");
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required.");
        }
    }

    public class CreateVaultSessionResponse : CfkApiResponse
    {
        public string? SessionId { get; set; }
        public VaultSession? Session { get; set; }
    }

    #endregion
}
