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
    /// Deletes the serialized Vault Graph for a given ProjectID.
    /// </summary>
    public class DeleteVaultGraphService : ApiServiceBase<DeleteVaultGraphRequest, DeleteVaultGraphResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultGraphService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultGraphResponse DoWork(DeleteVaultGraphRequest request)
        {
            var response = new DeleteVaultGraphResponse();

            try
            {
                var graph = Context.Set<VaultGraph>()
                    .FirstOrDefault(g => g.ProjectID == request.ProjectID);

                if (graph == null)
                {
                    response.UserMessage = $"No graph found for project '{request.ProjectID}'.";
                    response.Deleted = false;
                    return response;
                }

                Context.Set<VaultGraph>().Remove(graph);
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultGraph", graph.ID, "Deleted",
                    $"Graph deleted for project {request.ProjectID}");

                Context.Flush();

                _logger.Log(nameof(DeleteVaultGraphService), $"Deleted VaultGraph [{graph.ID}] for ProjectID={request.ProjectID}");

                response.Deleted = true;
                response.UserMessage = "Vault graph deleted successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultGraphService), "Error deleting vault graph.", ex);
                response.UserMessage = "An unexpected error occurred while deleting the vault graph.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultGraphRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string ProjectID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("ProjectID is required.");
        }
    }

    public class DeleteVaultGraphResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}
