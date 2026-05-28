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
    /// Deletes a VaultSession and its associated VaultFileRefs.
    /// </summary>
    public class DeleteVaultSessionService : ApiServiceBase<DeleteVaultSessionRequest, DeleteVaultSessionResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultSessionService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultSessionResponse DoWork(DeleteVaultSessionRequest request)
        {
            var response = new DeleteVaultSessionResponse();

            try
            {
                var session = Context.Set<VaultSession>()
                    .FirstOrDefault(s => s.ID == request.SessionId && s.VaultID == request.VaultId);

                if (session == null)
                {
                    response.Code        = 404;
                    response.UserMessage = $"Session '{request.SessionId}' not found.";
                    response.Deleted     = false;
                    return response;
                }

                var fileRefs = Context.Set<VaultFileRef>()
                    .Where(f => f.SessionID == request.SessionId)
                    .ToList();

                foreach (var fileRef in fileRefs)
                    Context.Set<VaultFileRef>().Remove(fileRef);

                Context.Set<VaultSession>().Remove(session);
                Context.Flush();

                response.Deleted     = true;
                response.UserMessage = "Session deleted successfully.";

                _logger.Log(nameof(DeleteVaultSessionService),
                    $"Deleted VaultSession [{session.ID}] '{session.Name}' and {fileRefs.Count} file ref(s).");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSession", session.ID, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultSessionService), "Error deleting VaultSession.", ex);
                response.Code        = 500;
                response.UserMessage = "An error occurred while deleting the session.";
                response.Deleted     = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultSessionRequest : CfkAuthorizedApiRequest
    {
        [Required]
        [MaxLength(128)]
        public string SessionId { get; set; }

        [Required]
        [MaxLength(128)]
        public string VaultId { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(SessionId))
                yield return new ValidationResult("SessionId is required.");
            if (string.IsNullOrWhiteSpace(VaultId))
                yield return new ValidationResult("VaultId is required.");
        }
    }

    public class DeleteVaultSessionResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}
