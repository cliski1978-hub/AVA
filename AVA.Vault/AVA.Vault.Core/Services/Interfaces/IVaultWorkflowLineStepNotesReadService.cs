using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineStepNotesReadService
    {
        Task<VaultAttachedNotesResponse> GetNotesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetRequiredNotesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetOptionalNotesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);
        Task<VaultAttachedNoteDto?> GetNoteForWorkflowLineStepAsync(string workflowLineStepId, string noteId, CancellationToken ct = default);
        Task<int> CountNotesForWorkflowLineStepAsync(string workflowLineStepId, CancellationToken ct = default);
    }
}
