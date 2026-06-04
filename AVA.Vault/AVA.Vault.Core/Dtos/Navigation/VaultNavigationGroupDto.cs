using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Navigation
{
    public class VaultNavigationGroupDto
    {
        public string GroupType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ColorKey { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public List<VaultNavigationItemDto> Items { get; set; } = new();
    }
}
