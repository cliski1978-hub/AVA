using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Files;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultFileUsageReadService
    {
        Task<VaultFileUsageDto> GetFileUsageAsync(string fileRefId, CancellationToken ct = default);
        Task<bool> CanSafelyDeleteFileAsync(string fileRefId, CancellationToken ct = default);
        Task<VaultFileUsageDto> GetFileUsageLocationsAsync(string fileRefId, CancellationToken ct = default);
    }
}
