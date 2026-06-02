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
    /// Deletes a VaultSessionFileRef link between a VaultSession and VaultFileRef.
    /// This does not delete the underlying VaultFileRef. Orphaned file cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultSessionFileRefService : ApiServiceBase<DeleteVaultSessionFileRefRequest, DeleteVaultSessionFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultSessionFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultSessionFileRefResponse DoWork(DeleteVaultSessionFileRefRequest request)
        {
            var response = new DeleteVaultSessionFileRefResponse();

            try
            {
                var sessionFileRef = Context.Set<VaultSessionFileRef>().FirstOrDefault(f => f.ID == request.SessionFileRefID);

                if (sessionFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault session file reference link not found.";
                    response.Deleted = false;
                    return response;
                }

                var sessionId = sessionFileRef.SessionID;
                var fileRefId = sessionFileRef.FileRefID;

                Context.Set<VaultSessionFileRef>().Remove(sessionFileRef);
                Context.Flush();

                response.Deleted = true;
                response.SessionFileRefID = request.SessionFileRefID;
                response.SessionID = sessionId;
                response.FileRefID = fileRefId;
                response.UserMessage = "Vault session file reference link deleted successfully.";

                _logger.Log(nameof(DeleteVaultSessionFileRefService), $"Deleted VaultSessionFileRef [{request.SessionFileRefID}] Session [{sessionId}] FileRef [{fileRefId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSessionFileRef", request.SessionFileRefID, "Deleted");

                // TODO: After file cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultSessionFileRefService), "Error deleting VaultSessionFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault session file reference link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultSessionFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string SessionFileRefID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(SessionFileRefID))
                yield return new ValidationResult("SessionFileRefID is required.");
        }
    }

    public class DeleteVaultSessionFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? SessionFileRefID { get; set; }
        public string? SessionID { get; set; }
        public string? FileRefID { get; set; }
    }

    #endregion
}