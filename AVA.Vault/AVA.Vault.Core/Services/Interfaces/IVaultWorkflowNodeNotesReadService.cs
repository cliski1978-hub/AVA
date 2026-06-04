using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultWorkflowNodeNotesReadService
    {
        Task<VaultAttachedNotesResponse> GetNotesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetRequiredNotesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetOptionalNotesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);
        Task<VaultAttachedNoteDto?> GetNoteForWorkflowNodeAsync(string workflowNodeId, string noteId, CancellationToken ct = default);
        Task<int> CountNotesForWorkflowNodeAsync(string workflowNodeId, CancellationToken ct = default);
    }
}
