using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultNoteContextReadService
    {
        Task<VaultNoteContextDto?> GetNoteContextAsync(string noteId, CancellationToken ct = default);
        Task<VaultNoteContextDto?> GetNoteContextSummaryAsync(string noteId, CancellationToken ct = default);
    }
}
