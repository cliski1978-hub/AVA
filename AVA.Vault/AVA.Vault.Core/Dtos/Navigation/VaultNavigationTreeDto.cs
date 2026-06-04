using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Navigation
{
    public class VaultNavigationTreeDto
    {
        public List<VaultNavigationVaultDto> Vaults { get; set; } = new();
    }
}
