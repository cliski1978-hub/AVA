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
    /// Updates an existing VaultWorkflowNote link between a VaultWorkflow and VaultNote.
    /// This does not update the underlying VaultNote content.
    /// </summary>
    public class UpdateVaultWorkflowNoteService : ApiServiceBase<UpdateVaultWorkflowNoteRequest, UpdateVaultWorkflowNoteResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowNoteResponse DoWork(UpdateVaultWorkflowNoteRequest request)
        {
            var response = new UpdateVaultWorkflowNoteResponse();

            try
            {
                var workflowNote = Context.Set<VaultWorkflowNote>().FirstOrDefault(n => n.ID == request.WorkflowNoteID);

                if (workflowNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowNote '{request.WorkflowNoteID}' not found.";
                    return response;
                }

                var workflowID = string.IsNullOrWhiteSpace(request.WorkflowID) ? workflowNote.WorkflowID : request.WorkflowID;
                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? workflowNote.NoteID : request.NoteID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowID) && request.WorkflowID != workflowNote.WorkflowID)
                {
                    var workflowExists = Context.Set<VaultWorkflow>().Any(w => w.ID == request.WorkflowID);

                    if (!workflowExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflow '{request.WorkflowID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != workflowNote.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.WorkflowID) && request.WorkflowID != workflowNote.WorkflowID) || (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != workflowNote.NoteID))
                {
                    var duplicateExists = Context.Set<VaultWorkflowNote>().Any(n => n.ID != workflowNote.ID && n.WorkflowID == workflowID && n.NoteID == noteID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This note is already linked to this workflow.";
                        return response;
                    }
                }

                workflowNote.WorkflowID = workflowID;
                workflowNote.NoteID = noteID;

                if (request.Instructions != null)
                    workflowNote.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    workflowNote.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    workflowNote.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    workflowNote.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    workflowNote.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowNote.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowNote.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowNote.IdentityList = request.IdentityList;

                workflowNote.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowNoteService), $"Updated VaultWorkflowNote [{workflowNote.ID}] Workflow [{workflowNote.WorkflowID}] Note [{workflowNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNote", workflowNote.ID, "Updated");

                response.WorkflowNoteID = workflowNote.ID;
                response.WorkflowNote = workflowNote;
                response.UserMessage = "Vault workflow note link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowNoteService), "Error updating VaultWorkflowNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow note link.";
            }

            return response;
        }
    }

    #region Models

    public class UpdateVaultWorkflowNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? WorkflowID { get; set; }

        [MaxLength(128)]
        public string? NoteID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowNoteID))
                yield return new ValidationResult("WorkflowNoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowNoteResponse : CfkApiResponse
    {
        public string? WorkflowNoteID { get; set; }
        public VaultWorkflowNote? WorkflowNote { get; set; }
    }

    #endregion
}