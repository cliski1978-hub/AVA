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
    /// Creates and persists a new VaultWorkflow.
    /// </summary>
    public class CreateVaultWorkflowService : ApiServiceBase<CreateVaultWorkflowRequest, CreateVaultWorkflowResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultWorkflowService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultWorkflowResponse DoWork(CreateVaultWorkflowRequest request)
        {
            var response = new CreateVaultWorkflowResponse();

            try
            {
                var projectExists = Context.Set<VaultProject>().Any(p => p.ID == request.ProjectID);

                if (!projectExists)
                {
                    response.Code = 404;
                    response.UserMessage = $"VaultProject [{request.ProjectID}] was not found.";
                    return response;
                }

                var exists = Context.Set<VaultWorkflow>().Any(w => w.ProjectID == request.ProjectID && w.Name.ToLower() == request.Name.ToLower());

                if (exists)
                {
                    response.Code = 400;
                    response.UserMessage = $"A workflow named '{request.Name}' already exists for this project.";
                    return response;
                }

                var workflow = new VaultWorkflow
                {
                    ID = Guid.NewGuid().ToString(),
                    Name = request.Name,
                    Description = request.Description,
                    WorkflowType = string.IsNullOrWhiteSpace(request.WorkflowType) ? "General" : request.WorkflowType,
                    Status = string.IsNullOrWhiteSpace(request.Status) ? "Active" : request.Status,
                    SortOrder = request.SortOrder,
                    ProjectID = request.ProjectID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PrimaryIdentityId = request.PrimaryIdentityId,
                    PrimaryIdentityHandle = request.PrimaryIdentityHandle,
                    PrimaryIdentityType = request.PrimaryIdentityType,
                    IdentityList = request.IdentityList
                    
                };

                Context.Set<VaultWorkflow>().Add(workflow);
                Context.Flush();

                // Set response before logging - if logging fails the created entity is still returned
                response.WorkflowID = workflow.ID;
                response.Workflow = workflow;
                response.UserMessage = "Vault workflow created successfully.";

                _logger.Log(nameof(CreateVaultWorkflowService), $"Created VaultWorkflow [{workflow.ID}] '{workflow.Name}'");

                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultWorkflow", workflow.ID, "Created");
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultWorkflowService), "Error creating VaultWorkflow.", ex);
                response.UserMessage = "An error occurred while creating the vault workflow.";
            }

            return response;
        }
    }

    #region Models

    public class CreateVaultWorkflowRequest : CfkAuthorizedApiRequest
    {

        [Required]
        [MaxLength(256)]
        public string Name { get; set; }

        public string? Description { get; set; }

        [MaxLength(64)]
        public string? WorkflowType { get; set; }

        [MaxLength(64)]
        public string? Status { get; set; }

        public int SortOrder { get; set; }

        [Required]
        [MaxLength(128)]
        public string ProjectID { get; set; }


        [Required]
        [MaxLength(128)]
        public string VaultID { get; set; }

        [Required]
        [MaxLength(128)]
        public string PrimaryIdentityId { get; set; }

        [Required]
        [MaxLength(64)]
        public string PrimaryIdentityHandle { get; set; }

        [Required]
        [MaxLength(32)]
        public string PrimaryIdentityType { get; set; }

        public byte[]? IdentityList { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(Name))
                yield return new ValidationResult("Name is required.");

            if (string.IsNullOrWhiteSpace(ProjectID))
               yield return new ValidationResult("ProjectID is required.");

            //if (string.IsNullOrWhiteSpace(PrimaryIdentityId))
               // yield return new ValidationResult("PrimaryIdentityId is required.");

           // if (string.IsNullOrWhiteSpace(PrimaryIdentityHandle))
               // yield return new ValidationResult("PrimaryIdentityHandle is required.");

            //if (string.IsNullOrWhiteSpace(PrimaryIdentityType))
                //yield return new ValidationResult("PrimaryIdentityType is required.");
        }
    }

    public class CreateVaultWorkflowResponse : CfkApiResponse
    {
        public string? WorkflowID { get; set; }
        public VaultWorkflow? Workflow { get; set; }
    }

    #endregion
}