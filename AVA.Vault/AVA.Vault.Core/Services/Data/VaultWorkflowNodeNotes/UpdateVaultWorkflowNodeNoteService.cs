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
    /// Updates an existing VaultWorkflowNodeNote link between a VaultWorkflowNode and VaultNote.
    /// This does not update the underlying VaultNote.
    /// </summary>
    public class UpdateVaultWorkflowNodeNoteService : ApiServiceBase<UpdateVaultWorkflowNodeNoteRequest, UpdateVaultWorkflowNodeNoteResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowNodeNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowNodeNoteResponse DoWork(UpdateVaultWorkflowNodeNoteRequest request)
        {
            var response = new UpdateVaultWorkflowNodeNoteResponse();

            try
            {
                var workflowNodeNote = Context.Set<VaultWorkflowNodeNote>().FirstOrDefault(n => n.ID == request.WorkflowNodeNoteID);

                if (workflowNodeNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowNodeNote '{request.WorkflowNodeNoteID}' not found.";
                    return response;
                }

                var workflowNodeID = string.IsNullOrWhiteSpace(request.WorkflowNodeID) ? workflowNodeNote.WorkflowNodeID : request.WorkflowNodeID;
                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? workflowNodeNote.NoteID : request.NoteID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowNodeID) && request.WorkflowNodeID != workflowNodeNote.WorkflowNodeID)
                {
                    var workflowNodeExists = Context.Set<VaultWorkflowNode>().Any(n => n.ID == request.WorkflowNodeID);

                    if (!workflowNodeExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflowNode '{request.WorkflowNodeID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != workflowNodeNote.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.WorkflowNodeID) && request.WorkflowNodeID != workflowNodeNote.WorkflowNodeID) || (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != workflowNodeNote.NoteID))
                {
                    var duplicateExists = Context.Set<VaultWorkflowNodeNote>().Any(n => n.ID != workflowNodeNote.ID && n.WorkflowNodeID == workflowNodeID && n.NoteID == noteID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This note is already linked to this workflow node.";
                        return response;
                    }
                }

                workflowNodeNote.WorkflowNodeID = workflowNodeID;
                workflowNodeNote.NoteID = noteID;

                if (request.Instructions != null)
                    workflowNodeNote.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    workflowNodeNote.IsRequired = request.IsRequired.Value;

                if (request.NoteOrder.HasValue)
                    workflowNodeNote.NoteOrder = request.NoteOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    workflowNodeNote.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    workflowNodeNote.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowNodeNote.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowNodeNote.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowNodeNote.IdentityList = request.IdentityList;

                workflowNodeNote.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowNodeNoteService), $"Updated VaultWorkflowNodeNote [{workflowNodeNote.ID}] WorkflowNode [{workflowNodeNote.WorkflowNodeID}] Note [{workflowNodeNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNodeNote", workflowNodeNote.ID, "Updated");

                response.WorkflowNodeNoteID = workflowNodeNote.ID;
                response.WorkflowNodeNote = workflowNodeNote;
                response.UserMessage = "Vault workflow node note link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowNodeNoteService), "Error updating VaultWorkflowNodeNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow node note link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultWorkflowNodeNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowNodeNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? NoteOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? WorkflowNodeID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowNodeNoteID))
                yield return new ValidationResult("WorkflowNodeNoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowNodeNoteResponse : CfkApiResponse
    {
        public string? WorkflowNodeNoteID { get; set; }
        public VaultWorkflowNodeNote? WorkflowNodeNote { get; set; }
    }

    #endregion
}