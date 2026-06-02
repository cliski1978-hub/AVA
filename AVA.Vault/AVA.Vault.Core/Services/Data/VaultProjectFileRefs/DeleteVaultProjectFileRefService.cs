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
    /// Deletes a VaultProjectFileRef link between a VaultProject and VaultFileRef.
    /// This does not delete the underlying VaultFileRef. Orphaned file cleanup will be centralized later.
    /// </summary>
    public class DeleteVaultProjectFileRefService : ApiServiceBase<DeleteVaultProjectFileRefRequest, DeleteVaultProjectFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultProjectFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultProjectFileRefResponse DoWork(DeleteVaultProjectFileRefRequest request)
        {
            var response = new DeleteVaultProjectFileRefResponse();

            try
            {
                var projectFileRef = Context.Set<VaultProjectFileRef>().FirstOrDefault(f => f.ID == request.ProjectFileRefID);

                if (projectFileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault project file reference link not found.";
                    response.Deleted = false;
                    return response;
                }

                var projectId = projectFileRef.ProjectID;
                var fileRefId = projectFileRef.FileRefID;

                Context.Set<VaultProjectFileRef>().Remove(projectFileRef);
                Context.Flush();

                response.Deleted = true;
                response.ProjectFileRefID = request.ProjectFileRefID;
                response.ProjectID = projectId;
                response.FileRefID = fileRefId;
                response.UserMessage = "Vault project file reference link deleted successfully.";

                _logger.Log(nameof(DeleteVaultProjectFileRefService), $"Deleted VaultProjectFileRef [{request.ProjectFileRefID}] Project [{projectId}] FileRef [{fileRefId}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProjectFileRef", request.ProjectFileRefID, "Deleted");

                // TODO: After file cleanup services are created, call centralized orphan evaluation here.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultProjectFileRefService), "Error deleting VaultProjectFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault project file reference link.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultProjectFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string ProjectFileRefID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ProjectFileRefID))
                yield return new ValidationResult("ProjectFileRefID is required.");
        }
    }

    public class DeleteVaultProjectFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? ProjectFileRefID { get; set; }
        public string? ProjectID { get; set; }
        public string? FileRefID { get; set; }
    }

    #endregion
}