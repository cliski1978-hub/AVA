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
    /// Updates an existing VaultSession's name, model attachments, canvas state, or metadata.
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
                var session = Context.Set<VaultSession>()
                    .FirstOrDefault(s => s.ID == request.SessionId && s.VaultID == request.VaultId);

                if (session == null)
                {
                    response.UserMessage = $"Session '{request.SessionId}' not found.";
                    return response;
                }

                if (!string.IsNullOrWhiteSpace(request.Name))
                    session.Name = request.Name;

                if (request.AttachedModelIdsJson != null)
                    session.AttachedModelIdsJson = request.AttachedModelIdsJson;

                if (request.BroadcastGroupIdsJson != null)
                    session.BroadcastGroupIdsJson = request.BroadcastGroupIdsJson;

                if (request.DefaultModelId != null)
                    session.DefaultModelId = request.DefaultModelId;

                if (request.CanvasJson != null)
                    session.CanvasJson = request.CanvasJson;

                session.LastActiveAt = DateTime.UtcNow;
                Context.Flush();

                _logger.Log(nameof(UpdateVaultSessionService),
                    $"Updated VaultSession [{session.ID}] '{session.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSession", session.ID, "Updated");

                response.SessionId   = session.ID;
                response.Session     = session;
                response.UserMessage = "Session updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultSessionService), "Error updating VaultSession.", ex);
                response.UserMessage = "An error occurred while updating the session.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultSessionRequest : CfkAuthorizedApiRequest
    {
        [Required]
        [MaxLength(128)]
        public string SessionId { get; set; }

        [Required]
        [MaxLength(128)]
        public string VaultId { get; set; }

        public string? Name { get; set; }
        public string? AttachedModelIdsJson { get; set; }
        public string? BroadcastGroupIdsJson { get; set; }
        public string? DefaultModelId { get; set; }
        public string? CanvasJson { get; set; }
        public bool? IsPinned { get; set; }
        public int? SpawnCount { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(SessionId))
                yield return new ValidationResult("SessionId is required.");
            if (string.IsNullOrWhiteSpace(VaultId))
                yield return new ValidationResult("VaultId is required.");
        }
    }

    public class UpdateVaultSessionResponse : CfkApiResponse
    {
        public string? SessionId { get; set; }
        public VaultSession? Session { get; set; }
    }

    #endregion
}
