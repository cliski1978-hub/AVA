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
    /// Deletes a VaultProject and cascades related VaultNotes and VaultGraph.
    /// </summary>
    public class DeleteVaultProjectService : ApiServiceBase<DeleteVaultProjectRequest, DeleteVaultProjectResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultProjectService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultProjectResponse DoWork(DeleteVaultProjectRequest request)
        {
            var response = new DeleteVaultProjectResponse();

            try
            {
                var project = Context.Set<VaultProject>()
                    .FirstOrDefault(p => p.ID == request.ProjectID && p.VaultID == request.VaultID);

                if (project == null)
                {
                    response.Code        = 404;
                    response.UserMessage = "Vault project not found.";
                    response.Deleted     = false;
                    return response;
                }

                // Delete related graph
                var graph = Context.Set<VaultGraph>().FirstOrDefault(g => g.ProjectID == project.ID);
                if (graph != null)
                    Context.Set<VaultGraph>().Remove(graph);

                // Delete related notes
                var notes = Context.Set<VaultNote>().Where(n => n.ProjectID == project.ID).ToList();
                foreach (var note in notes)
                    Context.Set<VaultNote>().Remove(note);

                // Delete sessions scoped to this project and their file refs
                var sessions = Context.Set<VaultSession>()
                    .Where(s => s.ProjectID == project.ID)
                    .ToList();

                var sessionIds = sessions.Select(s => s.ID).ToList();

                var fileRefs = Context.Set<VaultFileRef>()
                    .Where(f => sessionIds.Contains(f.SessionID))
                    .ToList();

                foreach (var fileRef in fileRefs)
                    Context.Set<VaultFileRef>().Remove(fileRef);

                foreach (var session in sessions)
                    Context.Set<VaultSession>().Remove(session);

                Context.Set<VaultProject>().Remove(project);
                Context.Flush();

                response.Deleted     = true;
                response.UserMessage = "Vault project and related data deleted successfully.";

                _logger.Log(nameof(DeleteVaultProjectService),
                    $"Deleted VaultProject [{project.ID}] '{project.Name}' and all related records from Vault {project.VaultID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultProject", project.ID, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultProjectService), "Error deleting VaultProject.", ex);
                response.Code        = 500;
                response.UserMessage = "An error occurred while deleting the vault project. " + ex.Message;
                response.Deleted     = false;
            }

            return response;
        }
    }

    #region Models
    public class DeleteVaultProjectRequest : CfkAuthorizedApiRequest
    {
        [Required] public string VaultID { get; set; }
        [Required] public string ProjectID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultID))
                yield return new ValidationResult("VaultID is required.");
            if (string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("ProjectID is required.");
        }
    }

    public class DeleteVaultProjectResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }
    #endregion
}
