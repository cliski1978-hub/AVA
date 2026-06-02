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
    /// Deletes a VaultHeaderFileRef link between a VaultHeader and VaultFileRef.
    /// This does not delete the underlying VaultFileRef. Orphaned file cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultHeaderFileRefService : ApiServiceBase<DeleteVaultHeaderFileRefRequest, DeleteVaultHeaderFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultHeaderFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultHeaderFileRefResponse DoWork(DeleteVaultHeaderFileRefRequest request)
        {
            var response = new DeleteVaultHeaderFileRefResponse();

            try
            {
                var headerFileRef = Context.Set<VaultHeaderFileRef>().FirstOrDefault(f => f.ID == request.HeaderFileRefID);

                if (headerFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault header file reference link not found.";
                    response.Deleted = false;
                    return response;
                }

                var vaultId = headerFileRef.VaultID;
                var fileRefId = headerFileRef.FileRefID;

                Context.Set<VaultHeaderFileRef>().Remove(headerFileRef);
                Context.Flush();

                response.Deleted = true;
                response.HeaderFileRefID = request.HeaderFileRefID;
                response.VaultID = vaultId;
                response.FileRefID = fileRefId;
                response.UserMessage = "Vault header file reference link deleted successfully.";

                _logger.Log(nameof(DeleteVaultHeaderFileRefService), $"Deleted VaultHeaderFileRef [{request.HeaderFileRefID}] Vault [{vaultId}] FileRef [{fileRefId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultHeaderFileRef", request.HeaderFileRefID, "Deleted");

                // TODO: After file cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultHeaderFileRefService), "Error deleting VaultHeaderFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault header file reference link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultHeaderFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string HeaderFileRefID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(HeaderFileRefID))
                yield return new ValidationResult("HeaderFileRefID is required.");
        }
    }

    public class DeleteVaultHeaderFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? HeaderFileRefID { get; set; }
        public string? VaultID { get; set; }
        public string? FileRefID { get; set; }
    }

    #endregion
}