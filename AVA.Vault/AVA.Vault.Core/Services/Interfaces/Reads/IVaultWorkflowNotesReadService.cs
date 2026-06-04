using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowNotesReadService
    {
        Task<VaultAttachedNotesResponse> GetNotesForWorkflowAsync(string workflowId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetRequiredNotesForWorkflowAsync(string workflowId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetOptionalNotesForWorkflowAsync(string workflowId, CancellationToken ct = default);
        Task<VaultAttachedNoteDto?> GetNoteForWorkflowAsync(string workflowId, string noteId, CancellationToken ct = default);
        Task<int> CountNotesForWorkflowAsync(string workflowId, CancellationToken ct = default);
    }
}
