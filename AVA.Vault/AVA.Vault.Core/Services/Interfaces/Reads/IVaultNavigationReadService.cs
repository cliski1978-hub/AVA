using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Dtos.Navigation;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultNavigationReadService
    {
        Task<VaultNavigationTreeDto> GetVaultNavigationTreeAsync(string vaultId, CancellationToken ct = default);
        Task<VaultNavigationTreeDto> GetAllVaultNavigationTreesAsync(CancellationToken ct = default);
        Task<VaultNavigationProjectDto> GetProjectNavigationBranchAsync(string projectId, CancellationToken ct = default);
    }
}
