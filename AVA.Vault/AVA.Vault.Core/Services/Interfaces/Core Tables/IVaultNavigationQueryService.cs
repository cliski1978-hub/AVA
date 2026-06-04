using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Models.Query;

namespace AVA.Vault.Core.Services.Interfaces
{
    public interface IVaultNavigationQueryService
    {
        Task<VaultNavigationResponse> GetNavigationTreeAsync(CancellationToken ct = default);
    }
}
