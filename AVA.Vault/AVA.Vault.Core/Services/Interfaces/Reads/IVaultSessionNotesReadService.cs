using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultSessionNotesReadService
    {
        Task<VaultAttachedNotesResponse> GetNotesForSessionAsync(string sessionId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetRequiredNotesForSessionAsync(string sessionId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetOptionalNotesForSessionAsync(string sessionId, CancellationToken ct = default);
        Task<VaultAttachedNoteDto?> GetNoteForSessionAsync(string sessionId, string noteId, CancellationToken ct = default);
        Task<int> CountNotesForSessionAsync(string sessionId, CancellationToken ct = default);
    }
}
