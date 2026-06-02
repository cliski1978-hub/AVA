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
    /// Updates an existing VaultWorkflowLineNote link between a VaultWorkflowLine and VaultNote.
    /// This does not update the underlying VaultNote.
    /// </summary>
    public class UpdateVaultWorkflowLineNoteService : ApiServiceBase<UpdateVaultWorkflowLineNoteRequest, UpdateVaultWorkflowLineNoteResponse>
    {
        private readonly VaultLogger _logger;

        public UpdateVaultWorkflowLineNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override UpdateVaultWorkflowLineNoteResponse DoWork(UpdateVaultWorkflowLineNoteRequest request)
        {
            var response = new UpdateVaultWorkflowLineNoteResponse();

            try
            {
                var workflowLineNote = Context.Set<VaultWorkflowLineNote>().FirstOrDefault(n => n.ID == request.WorkflowLineNoteID);

                if (workflowLineNote == null)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowLineNote '{request.WorkflowLineNoteID}' not found.";
                    return response;
                }

                var workflowLineID = string.IsNullOrWhiteSpace(request.WorkflowLineID) ? workflowLineNote.WorkflowLineID : request.WorkflowLineID;
                var noteID = string.IsNullOrWhiteSpace(request.NoteID) ? workflowLineNote.NoteID : request.NoteID;

                if (!string.IsNullOrWhiteSpace(request.WorkflowLineID) && request.WorkflowLineID != workflowLineNote.WorkflowLineID)
                {
                    var workflowLineExists = Context.Set<VaultWorkflowLine>().Any(l => l.ID == request.WorkflowLineID);

                    if (!workflowLineExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultWorkflowLine '{request.WorkflowLineID}' not found.";
                        return response;
                    }
                }

                if (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != workflowLineNote.NoteID)
                {
                    var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                    if (!noteExists)
                    {
                        response.Code = 404;
                        response.UserMessage = $"VaultNote '{request.NoteID}' not found.";
                        return response;
                    }
                }

                if ((!string.IsNullOrWhiteSpace(request.WorkflowLineID) && request.WorkflowLineID != workflowLineNote.WorkflowLineID) || (!string.IsNullOrWhiteSpace(request.NoteID) && request.NoteID != workflowLineNote.NoteID))
                {
                    var duplicateExists = Context.Set<VaultWorkflowLineNote>().Any(n => n.ID != workflowLineNote.ID && n.WorkflowLineID == workflowLineID && n.NoteID == noteID);

                    if (duplicateExists)
                    {
                        response.Code = 400;
                        response.UserMessage = "This note is already linked to this workflow line.";
                        return response;
                    }
                }

                workflowLineNote.WorkflowLineID = workflowLineID;
                workflowLineNote.NoteID = noteID;

                if (request.Instructions != null)
                    workflowLineNote.Instructions = request.Instructions;

                if (request.IsRequired.HasValue)
                    workflowLineNote.IsRequired = request.IsRequired.Value;

                if (request.SortOrder.HasValue)
                    workflowLineNote.SortOrder = request.SortOrder.Value;

                if (!string.IsNullOrWhiteSpace(request.UsageRole))
                    workflowLineNote.UsageRole = request.UsageRole;

                if (request.PrimaryIdentityId != null)
                    workflowLineNote.PrimaryIdentityId = request.PrimaryIdentityId;

                if (request.PrimaryIdentityHandle != null)
                    workflowLineNote.PrimaryIdentityHandle = request.PrimaryIdentityHandle;

                if (request.PrimaryIdentityType != null)
                    workflowLineNote.PrimaryIdentityType = request.PrimaryIdentityType;

                if (request.IdentityList != null)
                    workflowLineNote.IdentityList = request.IdentityList;

                workflowLineNote.UpdatedAt = DateTime.UtcNow;

                Context.Flush();

                _logger.Log(nameof(UpdateVaultWorkflowLineNoteService), $"Updated VaultWorkflowLineNote [{workflowLineNote.ID}] WorkflowLine [{workflowLineNote.WorkflowLineID}] Note [{workflowLineNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowLineNote", workflowLineNote.ID, "Updated");

                response.WorkflowLineNoteID = workflowLineNote.ID;
                response.WorkflowLineNote = workflowLineNote;
                response.UserMessage = "Vault workflow line note link updated successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(UpdateVaultWorkflowLineNoteService), "Error updating VaultWorkflowLineNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while updating the vault workflow line note link.";
            }

            return response;
        }
    }

    #region Update Models

    public class UpdateVaultWorkflowLineNoteRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string WorkflowLineNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool? IsRequired { get; set; }

        public int? SortOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [MaxLength(128)]
        public string? WorkflowLineID { get; set; }

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
            if (string.IsNullOrWhiteSpace(WorkflowLineNoteID))
                yield return new ValidationResult("WorkflowLineNoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class UpdateVaultWorkflowLineNoteResponse : CfkApiResponse
    {
        public string? WorkflowLineNoteID { get; set; }
        public VaultWorkflowLineNote? WorkflowLineNote { get; set; }
    }

    #endregion
}