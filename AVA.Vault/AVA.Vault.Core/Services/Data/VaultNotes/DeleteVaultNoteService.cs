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
    /// Safely deletes a VaultNote only when no relationship/link records still reference it.
    /// Relationship delete services should delete their own relationship row first, then call this service.
    /// Metadata is note-owned child data and is deleted with the note after relationship checks pass.
    /// </summary>
    public class DeleteVaultNoteService : ApiServiceBase<DeleteVaultNoteRequest, DeleteVaultNoteResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultNoteResponse DoWork(DeleteVaultNoteRequest request)
        {
            var response = new DeleteVaultNoteResponse();

            try
            {
                var note = Context.Set<VaultNote>().FirstOrDefault(n => n.ID == request.NoteID);

                if (note == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault note not found.";
                    response.Deleted = false;
                    response.NoteStillReferenced = false;
                    return response;
                }

                var fileRefNoteCount = Context.Set<VaultFileRefNote>().Count(n => n.NoteID == note.ID);
                var headerNoteCount = Context.Set<VaultHeaderNote>().Count(n => n.NoteID == note.ID);
                var incomingNoteRelationCount = Context.Set<VaultNoteRelation>().Count(r => r.TargetNoteID == note.ID);
                var outgoingNoteRelationCount = Context.Set<VaultNoteRelation>().Count(r => r.SourceNoteID == note.ID);
                var noteFileRefCount = Context.Set<VaultNoteFileRef>().Count(f => f.NoteID == note.ID);
                var projectNoteCount = Context.Set<VaultProjectNote>().Count(n => n.NoteID == note.ID);
                var sessionNoteCount = Context.Set<VaultSessionNote>().Count(n => n.NoteID == note.ID);
                var noteVaultTagCount = Context.Set<VaultNoteVaultTag>().Count(t => t.NoteID == note.ID);
                var workflowLineNoteCount = Context.Set<VaultWorkflowLineNote>().Count(n => n.NoteID == note.ID);
                var workflowLineStepNoteCount = Context.Set<VaultWorkflowLineStepNote>().Count(n => n.NoteID == note.ID);
                var workflowNodeNoteCount = Context.Set<VaultWorkflowNodeNote>().Count(n => n.NoteID == note.ID);
                var workflowNoteCount = Context.Set<VaultWorkflowNote>().Count(n => n.NoteID == note.ID);

                var relationshipCount =
                    fileRefNoteCount +
                    headerNoteCount +
                    incomingNoteRelationCount +
                    outgoingNoteRelationCount +
                    noteFileRefCount +
                    projectNoteCount +
                    sessionNoteCount +
                    noteVaultTagCount +
                    workflowLineNoteCount +
                    workflowLineStepNoteCount +
                    workflowNodeNoteCount +
                    workflowNoteCount;

                response.NoteID = note.ID;
                response.RelationshipCount = relationshipCount;
                response.FileRefNoteCount = fileRefNoteCount;
                response.HeaderNoteCount = headerNoteCount;
                response.IncomingNoteRelationCount = incomingNoteRelationCount;
                response.OutgoingNoteRelationCount = outgoingNoteRelationCount;
                response.NoteFileRefCount = noteFileRefCount;
                response.ProjectNoteCount = projectNoteCount;
                response.SessionNoteCount = sessionNoteCount;
                response.NoteVaultTagCount = noteVaultTagCount;
                response.WorkflowLineNoteCount = workflowLineNoteCount;
                response.WorkflowLineStepNoteCount = workflowLineStepNoteCount;
                response.WorkflowNodeNoteCount = workflowNodeNoteCount;
                response.WorkflowNoteCount = workflowNoteCount;

                if (relationshipCount > 0)
                {
                    response.Deleted = false;
                    response.NoteStillReferenced = true;
                    response.UserMessage = "Vault note was not deleted because it is still referenced by one or more relationship records.";

                    _logger.Log(nameof(DeleteVaultNoteService), $"Skipped deleting VaultNote [{note.ID}] '{note.Title}' because it still has [{relationshipCount}] relationship records.");

                    Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNote", note.ID, "Delete skipped - still referenced");

                    return response;
                }

                var metadata = Context.Set<VaultMetadata>().Where(m => m.NoteID == note.ID).ToList();

                foreach (var item in metadata)
                    Context.Set<VaultMetadata>().Remove(item);

                Context.Set<VaultNote>().Remove(note);
                Context.Flush();

                response.Deleted = true;
                response.NoteStillReferenced = false;
                response.MetadataDeletedCount = metadata.Count;
                response.UserMessage = "Vault note deleted successfully.";

                _logger.Log(nameof(DeleteVaultNoteService), $"Deleted VaultNote [{note.ID}] '{note.Title}' and [{metadata.Count}] metadata records.");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultNote", note.ID, "Deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultNoteService), "Error deleting VaultNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault note.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string NoteID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(NoteID))
                yield return new ValidationResult("NoteID is required.");
        }
    }

    public class DeleteVaultNoteResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public bool NoteStillReferenced { get; set; }
        public string? NoteID { get; set; }

        public int RelationshipCount { get; set; }

        public int FileRefNoteCount { get; set; }
        public int HeaderNoteCount { get; set; }
        public int IncomingNoteRelationCount { get; set; }
        public int OutgoingNoteRelationCount { get; set; }
        public int NoteFileRefCount { get; set; }
        public int ProjectNoteCount { get; set; }
        public int SessionNoteCount { get; set; }
        public int NoteVaultTagCount { get; set; }
        public int WorkflowLineNoteCount { get; set; }
        public int WorkflowLineStepNoteCount { get; set; }
        public int WorkflowNodeNoteCount { get; set; }
        public int WorkflowNoteCount { get; set; }

        public int MetadataDeletedCount { get; set; }
    }

    #endregion
}