using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Files;
using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultFileDetailsReadService
    {
        Task<VaultFileDetailsDto?> GetFileDetailsAsync(string fileRefId, CancellationToken ct = default);
        Task<VaultAttachedNotesResponse> GetFileNotesAsync(string fileRefId, CancellationToken ct = default);
        Task<VaultFileUsageDto> GetFileUsageAsync(string fileRefId, CancellationToken ct = default);
    }
}
