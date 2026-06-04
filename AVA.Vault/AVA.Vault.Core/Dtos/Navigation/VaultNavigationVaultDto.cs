using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Navigation
{
    public class VaultNavigationVaultDto
    {
        public string VaultID { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public VaultNavigationGroupDto NotesGroup { get; set; } = new();
        public VaultNavigationGroupDto SessionsGroup { get; set; } = new();
        public List<VaultNavigationProjectDto> Projects { get; set; } = new();
    }
}
