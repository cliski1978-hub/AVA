using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultNoteUsageReadService
    {
        Task<VaultNoteUsageDto> GetNoteUsageAsync(string noteId, CancellationToken ct = default);
        Task<bool> CanSafelyDeleteNoteAsync(string noteId, CancellationToken ct = default);
        Task<VaultNoteUsageDto> GetNoteUsageLocationsAsync(string noteId, CancellationToken ct = default);
    }
}
