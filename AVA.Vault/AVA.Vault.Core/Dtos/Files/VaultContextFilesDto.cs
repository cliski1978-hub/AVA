using AVA.Vault.Core.Dtos.Notes;

namespace AVA.Vault.Core.Dtos.Files
{
    public class VaultContextFilesDto
    {
        public string ContextID { get; set; } = string.Empty;
        public string ContextType { get; set; } = string.Empty;
        public int FileCount { get; set; }
        public int RequiredFileCount { get; set; }
        public int OptionalFileCount { get; set; }
        public VaultAttachedFilesResponse Files { get; set; } = null!;
    }
}
