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
    /// Safely deletes a VaultFileRef only when no relationship/link records still reference it.
    /// Relationship delete services should delete their own relationship row first, then call this service.
    /// This does not delete the physical file from disk.
    /// </summary>
    public class DeleteVaultFileRefService : ApiServiceBase<DeleteVaultFileRefRequest, DeleteVaultFileRefResponse>
    {
        private readonly VaultLogger _logger;

        public DeleteVaultFileRefService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override DeleteVaultFileRefResponse DoWork(DeleteVaultFileRefRequest request)
        {
            var response = new DeleteVaultFileRefResponse();

            try
            {
                var fileRef = Context.Set<VaultFileRef>().FirstOrDefault(f => f.ID == request.FileRefID);

                if (fileRef == null)
                {
                    response.Code = 404;
                    response.UserMessage = "Vault file reference not found.";
                    response.Deleted = false;
                    response.FileRefStillReferenced = false;
                    return response;
                }

                var sourceFileRefRelationCount = Context.Set<VaultFileRefRelation>().Count(r => r.SourceFileRefID == fileRef.ID);
                var targetFileRefRelationCount = Context.Set<VaultFileRefRelation>().Count(r => r.TargetFileRefID == fileRef.ID);
                var fileRefNoteCount = Context.Set<VaultFileRefNote>().Count(n => n.FileRefID == fileRef.ID);
                var headerFileRefCount = Context.Set<VaultHeaderFileRef>().Count(f => f.FileRefID == fileRef.ID);
                var projectFileRefCount = Context.Set<VaultProjectFileRef>().Count(f => f.FileRefID == fileRef.ID);
                var sessionFileRefCount = Context.Set<VaultSessionFileRef>().Count(f => f.FileRefID == fileRef.ID);
                var noteFileRefCount = Context.Set<VaultNoteFileRef>().Count(f => f.FileRefID == fileRef.ID);
                var workflowFileRefCount = Context.Set<VaultWorkflowFileRef>().Count(f => f.FileRefID == fileRef.ID);
                var workflowNodeFileRefCount = Context.Set<VaultWorkflowNodeFileRef>().Count(f => f.FileRefID == fileRef.ID);
                var workflowLineFileRefCount = Context.Set<VaultWorkflowLineFileRef>().Count(f => f.FileRefID == fileRef.ID);
                var workflowLineStepFileRefCount = Context.Set<VaultWorkflowLineStepFileRef>().Count(f => f.FileRefID == fileRef.ID);

                var relationshipCount =
                    sourceFileRefRelationCount +
                    targetFileRefRelationCount +
                    fileRefNoteCount +
                    headerFileRefCount +
                    projectFileRefCount +
                    sessionFileRefCount +
                    noteFileRefCount +
                    workflowFileRefCount +
                    workflowNodeFileRefCount +
                    workflowLineFileRefCount +
                    workflowLineStepFileRefCount;

                response.FileRefID = fileRef.ID;
                response.RelationshipCount = relationshipCount;
                response.SourceFileRefRelationCount = sourceFileRefRelationCount;
                response.TargetFileRefRelationCount = targetFileRefRelationCount;
                response.FileRefNoteCount = fileRefNoteCount;
                response.HeaderFileRefCount = headerFileRefCount;
                response.ProjectFileRefCount = projectFileRefCount;
                response.SessionFileRefCount = sessionFileRefCount;
                response.NoteFileRefCount = noteFileRefCount;
                response.WorkflowFileRefCount = workflowFileRefCount;
                response.WorkflowNodeFileRefCount = workflowNodeFileRefCount;
                response.WorkflowLineFileRefCount = workflowLineFileRefCount;
                response.WorkflowLineStepFileRefCount = workflowLineStepFileRefCount;

                if (relationshipCount > 0)
                {
                    response.Deleted = false;
                    response.FileRefStillReferenced = true;
                    response.UserMessage = "Vault file reference was not deleted because it is still referenced by one or more relationship records.";

                    _logger.Log(nameof(DeleteVaultFileRefService), $"Skipped deleting VaultFileRef [{fileRef.ID}] '{fileRef.Name}' because it still has [{relationshipCount}] relationship records.");

                    Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRef", fileRef.ID, "Delete skipped - still referenced");

                    return response;
                }

                Context.Set<VaultFileRef>().Remove(fileRef);
                Context.Flush();

                response.Deleted = true;
                response.FileRefStillReferenced = false;
                response.UserMessage = "Vault file reference deleted successfully.";

                _logger.Log(nameof(DeleteVaultFileRefService), $"Deleted VaultFileRef [{fileRef.ID}] '{fileRef.Name}'.");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultFileRef", fileRef.ID, "Deleted");

                // Physical file deletion should be handled by a separate storage/file service, not this database service.
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(DeleteVaultFileRefService), "Error deleting VaultFileRef.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while deleting the vault file reference.";
                response.Deleted = false;
            }

            return response;
        }
    }

    #region Models

    public class DeleteVaultFileRefRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string FileRefID { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(FileRefID))
                yield return new ValidationResult("FileRefID is required.");
        }
    }

    public class DeleteVaultFileRefResponse : CfkApiResponse
    {
        public bool Deleted { get; set; }
        public bool FileRefStillReferenced { get; set; }
        public string? FileRefID { get; set; }

        public int RelationshipCount { get; set; }

        public int SourceFileRefRelationCount { get; set; }
        public int TargetFileRefRelationCount { get; set; }
        public int FileRefNoteCount { get; set; }
        public int HeaderFileRefCount { get; set; }
        public int ProjectFileRefCount { get; set; }
        public int SessionFileRefCount { get; set; }
        public int NoteFileRefCount { get; set; }
        public int WorkflowFileRefCount { get; set; }
        public int WorkflowNodeFileRefCount { get; set; }
        public int WorkflowLineFileRefCount { get; set; }
        public int WorkflowLineStepFileRefCount { get; set; }
    }

    #endregion
}