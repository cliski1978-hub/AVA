
using System.ComponentModel.DataAnnotations;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Logger;
using AVA.Vault.Core.Utils;
using AVA.Vault.Core.Graph;
using CliskiCore.DbAPI.Interfaces;
using CliskiCore.DbAPI;

namespace AVA.Vault.Core.Services.Data
{
    /// <summary>
    /// Creates and persists a serialized Vault Graph for a given project.
    /// </summary>
    public class CreateVaultGraphService : ApiServiceBase<CreateVaultGraphRequest, CreateVaultGraphResponse>
    {
        private readonly VaultLogger _logger;

        public CreateVaultGraphService(IDbContext context, VaultLogger logger) : base(context)
        {
            _logger = logger;
        }

        protected override CreateVaultGraphResponse DoWork(CreateVaultGraphRequest request)
        {
            var response = new CreateVaultGraphResponse();

            try
            {
                var existing = Context.Set<VaultGraph>()
                    .FirstOrDefault(g => g.ProjectID == request.ProjectID);

                if (existing != null)
                {
                    response.UserMessage = $"A graph already exists for project '{request.ProjectID}'.";
                    response.GraphID = existing.ID;
                    return response;
                }

                var graph = new VaultGraph
                {
                    ID = Guid.NewGuid().ToString(),
                    ProjectID = request.ProjectID,
                    GraphData = VaultSerializer.ToJson(request.Graph ?? new NoteGraph()),
                    GeneratedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Context.Set<VaultGraph>().Add(graph);
                Context.Flush();

                _logger.Log(nameof(CreateVaultGraphService), $"Created new VaultGraph for ProjectID={request.ProjectID}");
                Context.Log(request.RequestPartyName, LogLevel.Summary, "VaultGraph", graph.ID, "Created");

                response.GraphID = graph.ID;
                response.Graph = request.Graph;
                response.UserMessage = "Vault graph created successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(nameof(CreateVaultGraphService), "Error creating vault graph.", ex);
                response.UserMessage = "An unexpected error occurred while creating the vault graph.";
            }

            return response;
        }
    }

    #region Models

    /// <summary>
    /// Request to create a serialized Vault Graph for a project.
    /// </summary>
    public class CreateVaultGraphRequest : CfkAuthorizedApiRequest
    {
        [Required]
        public string ProjectID { get; set; }

        /// <summary>
        /// The NoteGraph structure to serialize and store.
        /// </summary>
        public NoteGraph? Graph { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(ProjectID))
                yield return new ValidationResult("ProjectID is required.");
        }
    }

    /// <summary>
    /// Response returned after creating a Vault Graph.
    /// </summary>
    public class CreateVaultGraphResponse : CfkApiResponse
    {
        public string? GraphID { get; set; }
        public NoteGraph? Graph { get; set; }
    }

    #endregion
}
