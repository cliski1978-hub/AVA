using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultProjectNotesReadService
    {
        Task<VaultAttachedNotesResponse> GetNotesForProjectAsync(string projectId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetRequiredNotesForProjectAsync(string projectId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetOptionalNotesForProjectAsync(string projectId, CancellationToken ct = default);
        Task<VaultAttachedNoteDto?> GetNoteForProjectAsync(string projectId, string noteId, CancellationToken ct = default);
        Task<int> CountNotesForProjectAsync(string projectId, CancellationToken ct = default);
    }
}
