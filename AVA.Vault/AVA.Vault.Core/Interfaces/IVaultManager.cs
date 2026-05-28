using AVA.Vault.Core.Data.Models;

namespace AVA.Vault.Core.Interfaces
{
    public interface IVaultManager
    {
        Services.VaultService CreateVault(string name);
        Services.VaultService LoadVault(string path);
        Services.VaultService GetVaultById(string vaultId);
        IEnumerable<VaultHeader> ListVaults();
        void DeleteVault(string vaultId);
    }
}
