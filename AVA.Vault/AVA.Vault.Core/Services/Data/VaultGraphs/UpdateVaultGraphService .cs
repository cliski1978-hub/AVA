using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Graph;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Utils;
using CliskiCore.DbAPI;
using CliskiCore.DbAPI.Interfaces;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Updates (replaces) the serialized Vault Graph for a given ProjectID.
    /// </summary>
    public class UpdateVaultGraphService : ApiServiceBase<UpdateVaultGraphRequest, UpdateVaultGraphResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultGraphService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultGraphResponse DoWork(UpdateVaultGraphRequest request)
        {
            var response = new UpdateVaultGraphResponse();

            try
            {
                var graph = Context.Set<VaultGraph>()
                    .FirstOrDefault(g => g.ProjectID == request.ProjectID);

                if (graph == null)
                {
                    response.UserMessage = $"No graph exists for project '{request.ProjectID}'.";
                    return response;
                }

                // Replace serialized graph payload
                graph.GraphData = VaultSerializer.ToJson(request.Graph ?? new NoteGraph());
                // Keep original GeneratedAt (first creation time), but update UpdatedAt
                graph.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultGraphService), $"Updated VaultGraph for ProjectID={request.ProjectID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultGraph", graph.ID, "Updated",
                    $"Graph updated for project {request.ProjectID}");

                response.GraphID = graph.ID;
                response.Graph = request.Graph;
                response.UserMessage = "Vault graph updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultGraphService), "Error updating vault graph.", ex);
                response.UserMessage = "An unexpected error occurred while updating the vault graph.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultGraphRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string ProjectID { get; set; }

        /// <summary>
        /// Full replacement graph to store.
        /// </summary>
        [Required]
        public NoteGraph Graph { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("ProjectID is required.");
            if (Graph == null)
                yield return new ValidationResult("Graph is required.");
        }
    }

    public class UpdateVaultGraphResponse : CfkApiResponse
    {
        public string? GraphID { get; set; }
        public NoteGraph? Graph { get; set; }
    }

    #endregion
}
