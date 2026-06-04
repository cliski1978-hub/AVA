using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultHeaderNotesReadService
    {
        Task<VaultAttachedNotesResponse> GetNotesForVaultAsync(string vaultId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetRequiredNotesForVaultAsync(string vaultId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetOptionalNotesForVaultAsync(string vaultId, CancellationToken ct = default);
        Task<VaultAttachedNoteDto?> GetNoteForVaultAsync(string vaultId, string noteId, CancellationToken ct = default);
        Task<int> CountNotesForVaultAsync(string vaultId, CancellationToken ct = default);
    }
}
