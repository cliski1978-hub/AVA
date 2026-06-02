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
    /// Updates an existing VaultWorkflowLineStepNote link between a VaultWorkflowLineStep and VaultNote.
    /// This does not update the underlying VaultNote.
    /// </summary>
    public class UpdateVaultWorkflowLineStepNoteService : ApiServiceBase<UpdateVaultWorkflowLineStepNoteRequest, UpdateVaultWorkflowLineStepNoteResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowLineStepNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowLineStepNoteResponse DoWork(UpdateVaultWorkflowLineStepNoteRequest request)
        {
            var response = new UpdateVaultWorkflowLineStepNoteResponse();

            try
            {
                var workflowLineStepNote = Context.Set<VaultWorkflowLineStepNote>().FirstOrDefault(n => n.ID == request.WorkflowLineStepNoteID);

                if (workflowLineStepNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLineStepNote '{request.WorkflowLineStepNoteID}' not found.";
                    return response;
                }

                var workflowLineStepID = string.IsNullOrWhiteSpace(request.WorkflowLineStepID) ? workflowLineStepNote.WorkflowLineStepID : request.WorkflowLineStepID;
                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? workflowLineStepNote.NoteID : request.NoteID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowLineStepID) && request.WorkflowLineStepID != workflowLineStepNote.WorkflowLineStepID)
                {
                    var workflowLineStepExists = Context.Set<VaultWorkflowLineStep>().Any(s => s.ID == request.WorkflowLineStepID);

                    if (!workflowLineStepExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflowLineStep '{request.WorkflowLineStepID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != workflowLineStepNote.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.WorkflowLineStepID) && request.WorkflowLineStepID != workflowLineStepNote.WorkflowLineStepID) || (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != workflowLineStepNote.NoteID))
                {
                    var duplicateExists = Context.Set<VaultWorkflowLineStepNote>().Any(n => n.ID != workflowLineStepNote.ID && n.WorkflowLineStepID == workflowLineStepID && n.NoteID == noteID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This note is already linked to this workflow line step.";
                        return response;
                    }
                }

                workflowLineStepNote.WorkflowLineStepID = workflowLineStepID;
                workflowLineStepNote.NoteID = noteID;

                if (request.Instructions != null)
                    workflowLineStepNote.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    workflowLineStepNote.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    workflowLineStepNote.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    workflowLineStepNote.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    workflowLineStepNote.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowLineStepNote.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowLineStepNote.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowLineStepNote.IdentityList = request.IdentityList;

                workflowLineStepNote.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowLineStepNoteService), $"Updated VaultWorkflowLineStepNote [{workflowLineStepNote.ID}] WorkflowLineStep [{workflowLineStepNote.WorkflowLineStepID}] Note [{workflowLineStepNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineStepNote", workflowLineStepNote.ID, "Updated");

                response.WorkflowLineStepNoteID = workflowLineStepNote.ID;
                response.WorkflowLineStepNote = workflowLineStepNote;
                response.UserMessage = "Vault workflow line step note link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowLineStepNoteService), "Error updating VaultWorkflowLineStepNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow line step note link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultWorkflowLineStepNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineStepNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? WorkflowLineStepID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowLineStepNoteID))
                yield return new ValidationResult("WorkflowLineStepNoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowLineStepNoteResponse : CfkApiResponse
    {
        public string? WorkflowLineStepNoteID { get; set; }
        public VaultWorkflowLineStepNote? WorkflowLineStepNote { get; set; }
    }

    #endregion
}