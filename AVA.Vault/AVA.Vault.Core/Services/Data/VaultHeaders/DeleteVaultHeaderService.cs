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
    /// Deletes a VaultHeader and cascades related VaultProjects, VaultNotes, VaultGraphs, VaultTags, VaultNoteRelations, and VaultMetadata.
    /// </summary>
    public class DeleteVaultHeaderService : ApiServiceBase<DeleteVaultHeaderRequest, DeleteVaultHeaderResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultHeaderService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultHeaderResponse DoWork(DeleteVaultHeaderRequest request)
        {
            var response = new DeleteVaultHeaderResponse();

            try
            {
                var vault = Context.Set<VaultHeader>()
                    .FirstOrDefault(v => v.ID == request.VaultId);

                if (vault == null)
                {
                    response.Code        = 404;
                    response.UserMessage = "Vault not found.";
                    response.Deleted     = false;
                    return response;
                }

                var projects = Context.Set<VaultProject>()
                    .Where(p => p.VaultID == vault.ID)
                    .ToList();

                var projectIds = projects.Select(p => p.ID).ToList();

                var notes = Context.Set<VaultNote>()
                    .Where(n => n.VaultID == vault.ID)
                    .ToList();

                var noteIds = notes.Select(n => n.ID).ToList();

                var graphs = Context.Set<VaultGraph>()
                    .Where(g => projectIds.Contains(g.ProjectID))
                    .ToList();

                var relations = Context.Set<VaultNoteRelation>()
                    .Where(r => noteIds.Contains(r.SourceNoteID) ||
                                noteIds.Contains(r.TargetNoteID))
                    .ToList();

                var metadata = Context.Set<VaultMetadata>()
                    .Where(m => noteIds.Contains(m.NoteID))
                    .ToList();

                var tags = Context.Set<VaultTag>()
                    .Where(t => t.ProjectID == vault.ID)
                    .ToList();

                // All sessions belonging to this vault (vault-level and project-level)
                var sessions = Context.Set<VaultSession>()
                    .Where(s => s.VaultID == vault.ID)
                    .ToList();

                var sessionIds = sessions.Select(s => s.ID).ToList();

                var fileRefs = Context.Set<VaultFileRef>()
                    .Where(f => sessionIds.Contains(f.SessionID))
                    .ToList();

                foreach (var relation in relations)
                    Context.Set<VaultNoteRelation>().Remove(relation);

                foreach (var item in metadata)
                    Context.Set<VaultMetadata>().Remove(item);

                foreach (var graph in graphs)
                    Context.Set<VaultGraph>().Remove(graph);

                foreach (var note in notes)
                    Context.Set<VaultNote>().Remove(note);

                foreach (var tag in tags)
                    Context.Set<VaultTag>().Remove(tag);

                foreach (var fileRef in fileRefs)
                    Context.Set<VaultFileRef>().Remove(fileRef);

                foreach (var session in sessions)
                    Context.Set<VaultSession>().Remove(session);

                foreach (var project in projects)
                    Context.Set<VaultProject>().Remove(project);

                Context.Set<VaultHeader>().Remove(vault);
                Context.Flush();

                response.Deleted     = true;
                response.UserMessage = "Vault and related data deleted successfully.";

                _logger.Log(nameof(DeleteVaultHeaderService),
                    $"Deleted VaultHeader [{vault.ID}] '{vault.DisplayName}' and all related records.");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultHeader", vault.ID, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultHeaderService), "Error deleting VaultHeader.", ex);
                response.Code        = 500;
                response.UserMessage = "An error occurred while deleting the vault.";
                response.Deleted     = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultHeaderRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string VaultId { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(VaultId))
                yield return new ValidationResult("VaultId is required.");
        }
    }

    public class DeleteVaultHeaderResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
    }

    #endregion
}