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
    /// Creates and persists a new VaultWorkflowNodeNote link between a VaultWorkflowNode and VaultNote.
    /// </summary>
    public class CreateVaultWorkflowNodeNoteService : ApiServiceBase<CreateVaultWorkflowNodeNoteRequest, CreateVaultWorkflowNodeNoteResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowNodeNoteService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowNodeNoteResponse DoWork(CreateVaultWorkflowNodeNoteRequest request)
        {
            var response = new CreateVaultWorkflowNodeNoteResponse();

            try
            {
                var workflowNodeExists = Context.Set<VaultWorkflowNode>().Any(n => n.ID == request.WorkflowNodeID);

                if (!workflowNodeExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultWorkflowNode [{request.WorkflowNodeID}] was not found.";
                    return response;
                }

                var noteExists = Context.Set<VaultNote>().Any(n => n.ID == request.NoteID);

                if (!noteExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultNote [{request.NoteID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflowNodeNote>().Any(n => n.ID == request.WorkflowNodeNoteID || (n.WorkflowNodeID == request.WorkflowNodeID && n.NoteID == request.NoteID));

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = "This note is already linked to this workflow node.";
                    return response;
                }

                var workflowNodeNote = new VaultWorkflowNodeNote
                {
                    ID = string.IsNullOrWhiteSpace(request.WorkflowNodeNoteID) ? Guid.NewGuid().ToString() : request.WorkflowNodeNoteID,
                    Instructions = request.Instructions,
                    IsRequired = request.IsRequired,
                    NoteOrder = request.NoteOrder,
                    UsageRole = string.IsNullOrWhiteSpace(request.UsageRole) ? "Reference" : request.UsageRole,
                    WorkflowNodeID = request.WorkflowNodeID,
                    NoteID = request.NoteID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                };

                Context.Set<VaultWorkflowNodeNote>().Add(workflowNodeNote);
                Context.Flush();

                response.WorkflowNodeNoteID = workflowNodeNote.ID;
                response.WorkflowNodeNote = workflowNodeNote;
                response.UserMessage = "Vault workflow node note link created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowNodeNoteService), $"Created VaultWorkflowNodeNote [{workflowNodeNote.ID}] WorkflowNode [{workflowNodeNote.WorkflowNodeID}] Note [{workflowNodeNote.NoteID}]");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflowNodeNote", workflowNodeNote.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowNodeNoteService), "Error creating VaultWorkflowNodeNote.", ex);
                response.Code = 500;
                response.UserMessage = "An error occurred while creating the vault workflow node note link.";
            }

            return response;
        }
    }

    #region Create Models

    public class CreateVaultWorkflowNodeNoteRequest : CfkAuthorizedApiRequest
    {
        public string? WorkflowNodeNoteID { get; set; }

        public string? Instructions { get; set; }

        public bool IsRequired { get; set; }

        public int NoteOrder { get; set; }

        [MaxLength(64)]
        public string? UsageRole { get; set; }

        [Required]
        [MaxLength(128)]
        public string WorkflowNodeID { get; set; }

        [Required]
        [MaxLength(128)]
        public string NoteID { get; set; }

        [MaxLength(128)]
        public string? PrimaryIdentityId { get; set; }

        [MaxLength(64)]
        public string? PrimaryIdentityHandle { get; set; }

        [MaxLength(32)]
        public string? PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(WorkflowNodeID))
                yield return new ValidationResult("WorkflowNodeID is required.");

            if (string.IsNullOrWhiteSpace(NoteID))
                yield return new ValidationResult("NoteID is required.");

            // Identity validation is intentionally disabled until the identity layer is wired in.
            // if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
            //     yield return new ValidationResult("PrimaryIdentityId is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
            //     yield return new ValidationResult("PrimaryIdentityHandle is required.");

            // if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
            //     yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultWorkflowNodeNoteResponse : CfkApiResponse
    {
        public string? WorkflowNodeNoteID { get; set; }
        public VaultWorkflowNodeNote? WorkflowNodeNote { get; set; }
    }

    #endregion
}