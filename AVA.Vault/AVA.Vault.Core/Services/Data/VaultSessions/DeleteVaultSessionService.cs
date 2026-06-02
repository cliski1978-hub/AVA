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
    /// Deletes a VaultSession and session-owned/link records.
    /// This does not delete underlying VaultNote records. Note cleanup will be centralized later.
    /// FileRef cleanup may need to be revisited when centralized file orphan cleanup is wired in.
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
                var session = Context.Set<VaultSession>().FirstOrDefault(s => s.ID == request.SessionID);

                if (session == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault session not found.";
                    response.Deleted = false;
                    return response;
                }

                var sessionNotes = Context.Set<VaultSessionNote>().Where(n => n.SessionID == session.ID).ToList();
                var sessionFileRefs = Context.Set<VaultSessionFileRef>().Where(f => f.SessionID == session.ID).ToList();

                var directNotes = Context.Set<VaultNote>().Where(n => n.SessionID == session.ID).ToList();
                var directNoteIds = directNotes.Select(n => n.ID).ToList();

                var fileRefs = Context.Set<VaultFileRef>().Where(f => f.SessionID == session.ID).ToList();
                var fileRefIds = fileRefs.Select(f => f.ID).ToList();

                var fileRefRelations = Context.Set<VaultFileRefRelation>().Where(r => fileRefIds.Contains(r.SourceFileRefID) || fileRefIds.Contains(r.TargetFileRefID)).ToList();
                var fileRefNotes = Context.Set<VaultFileRefNote>().Where(n => fileRefIds.Contains(n.FileRefID)).ToList();
                var headerFileRefs = Context.Set<VaultHeaderFileRef>().Where(f => fileRefIds.Contains(f.FileRefID)).ToList();
                var projectFileRefs = Context.Set<VaultProjectFileRef>().Where(f => fileRefIds.Contains(f.FileRefID)).ToList();
                var noteFileRefsByFile = Context.Set<VaultNoteFileRef>().Where(f => fileRefIds.Contains(f.FileRefID)).ToList();
                var workflowFileRefs = Context.Set<VaultWorkflowFileRef>().Where(f => fileRefIds.Contains(f.FileRefID)).ToList();
                var workflowNodeFileRefs = Context.Set<VaultWorkflowNodeFileRef>().Where(f => fileRefIds.Contains(f.FileRefID)).ToList();
                var workflowLineFileRefs = Context.Set<VaultWorkflowLineFileRef>().Where(f => fileRefIds.Contains(f.FileRefID)).ToList();
                var workflowLineStepFileRefs = Context.Set<VaultWorkflowLineStepFileRef>().Where(f => fileRefIds.Contains(f.FileRefID)).ToList();

                var fileRefNotesByNote = Context.Set<VaultFileRefNote>().Where(n => directNoteIds.Contains(n.NoteID)).ToList();
                var headerNotes = Context.Set<VaultHeaderNote>().Where(n => directNoteIds.Contains(n.NoteID)).ToList();
                var projectNotes = Context.Set<VaultProjectNote>().Where(n => directNoteIds.Contains(n.NoteID)).ToList();
                var sessionNotesByNote = Context.Set<VaultSessionNote>().Where(n => directNoteIds.Contains(n.NoteID)).ToList();
                var workflowNotes = Context.Set<VaultWorkflowNote>().Where(n => directNoteIds.Contains(n.NoteID)).ToList();
                var workflowNodeNotes = Context.Set<VaultWorkflowNodeNote>().Where(n => directNoteIds.Contains(n.NoteID)).ToList();
                var workflowLineNotes = Context.Set<VaultWorkflowLineNote>().Where(n => directNoteIds.Contains(n.NoteID)).ToList();
                var workflowLineStepNotes = Context.Set<VaultWorkflowLineStepNote>().Where(n => directNoteIds.Contains(n.NoteID)).ToList();
                var noteFileRefsByNote = Context.Set<VaultNoteFileRef>().Where(f => directNoteIds.Contains(f.NoteID)).ToList();
                var noteRelations = Context.Set<VaultNoteRelation>().Where(r => directNoteIds.Contains(r.SourceNoteID) || directNoteIds.Contains(r.TargetNoteID)).ToList();
                var noteVaultTags = Context.Set<VaultNoteVaultTag>().Where(t => directNoteIds.Contains(t.NoteID)).ToList();
                var metadata = Context.Set<VaultMetadata>().Where(m => directNoteIds.Contains(m.NoteID)).ToList();

                foreach (var item in fileRefRelations)
                    Context.Set<VaultFileRefRelation>().Remove(item);

                foreach (var item in fileRefNotes)
                    Context.Set<VaultFileRefNote>().Remove(item);

                foreach (var item in headerFileRefs)
                    Context.Set<VaultHeaderFileRef>().Remove(item);

                foreach (var item in projectFileRefs)
                    Context.Set<VaultProjectFileRef>().Remove(item);

                foreach (var item in noteFileRefsByFile)
                    Context.Set<VaultNoteFileRef>().Remove(item);

                foreach (var item in workflowFileRefs)
                    Context.Set<VaultWorkflowFileRef>().Remove(item);

                foreach (var item in workflowNodeFileRefs)
                    Context.Set<VaultWorkflowNodeFileRef>().Remove(item);

                foreach (var item in workflowLineFileRefs)
                    Context.Set<VaultWorkflowLineFileRef>().Remove(item);

                foreach (var item in workflowLineStepFileRefs)
                    Context.Set<VaultWorkflowLineStepFileRef>().Remove(item);

                foreach (var item in fileRefs)
                    Context.Set<VaultFileRef>().Remove(item);

                foreach (var item in fileRefNotesByNote)
                    Context.Set<VaultFileRefNote>().Remove(item);

                foreach (var item in headerNotes)
                    Context.Set<VaultHeaderNote>().Remove(item);

                foreach (var item in projectNotes)
                    Context.Set<VaultProjectNote>().Remove(item);

                foreach (var item in sessionNotesByNote)
                    Context.Set<VaultSessionNote>().Remove(item);

                foreach (var item in workflowNotes)
                    Context.Set<VaultWorkflowNote>().Remove(item);

                foreach (var item in workflowNodeNotes)
                    Context.Set<VaultWorkflowNodeNote>().Remove(item);

                foreach (var item in workflowLineNotes)
                    Context.Set<VaultWorkflowLineNote>().Remove(item);

                foreach (var item in workflowLineStepNotes)
                    Context.Set<VaultWorkflowLineStepNote>().Remove(item);

                foreach (var item in noteFileRefsByNote)
                    Context.Set<VaultNoteFileRef>().Remove(item);

                foreach (var item in noteRelations)
                    Context.Set<VaultNoteRelation>().Remove(item);

                foreach (var item in noteVaultTags)
                    Context.Set<VaultNoteVaultTag>().Remove(item);

                foreach (var item in metadata)
                    Context.Set<VaultMetadata>().Remove(item);

                foreach (var item in directNotes)
                    Context.Set<VaultNote>().Remove(item);

                foreach (var item in sessionNotes)
                    Context.Set<VaultSessionNote>().Remove(item);

                foreach (var item in sessionFileRefs)
                    Context.Set<VaultSessionFileRef>().Remove(item);

                Context.Set<VaultSession>().Remove(session);
                Context.Flush();

                response.Deleted = true;
                response.SessionID = request.SessionID;
                response.UserMessage = "Vault session and related session data deleted successfully.";

                _logger.Log(nameof(DeleteVaultSessionService), $"Deleted VaultSession [{request.SessionID}] '{session.Name}' and related session records.");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultSession", request.SessionID, "Deleted");

                // TODO: Revisit this delete after DeleteVaultNoteService and centralized orphan cleanup services are wired in.
                // TODO: Revisit direct VaultFileRef deletion after centralized file orphan cleanup is wired in.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultSessionService), "Error deleting VaultSession.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault session.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Delete Models

    public class DeleteVaultSessionRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string SessionID { get; set; }

        [MaxLength(128)]
        public string? VaultID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(SessionID))
                yield return new ValidationResult("SessionID is required.");
        }
    }

    public class DeleteVaultSessionResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public string? SessionID { get; set; }
    }

    #endregion
}