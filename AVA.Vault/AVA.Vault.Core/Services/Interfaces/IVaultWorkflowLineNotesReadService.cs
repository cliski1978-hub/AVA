using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowLineNotesReadService
    {
        Task<VaultAttachedNotesResponse> GetNotesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetRequiredNotesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetOptionalNotesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);
        Task<VaultAttachedNoteDto?> GetNoteForWorkflowLineAsync(string workflowLineId, string noteId, CancellationToken ct = default);
        Task<int> CountNotesForWorkflowLineAsync(string workflowLineId, CancellationToken ct = default);
    }
}
