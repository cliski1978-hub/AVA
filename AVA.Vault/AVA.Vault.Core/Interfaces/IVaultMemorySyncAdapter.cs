using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Interfaces
{
    public interface IVaultMemorySyncAdapter
    {
        Task SyncAllAsync(CancellationToken ct = default);
        Task PushToMemoryAsync(VaultNote note, CancellationToken ct = default);
        Task PullFromMemoryAsync(CancellationToken ct = default);
    }
}
