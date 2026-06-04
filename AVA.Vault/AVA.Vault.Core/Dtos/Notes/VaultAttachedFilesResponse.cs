using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultAttachedFilesResponse
    {
        public string ParentID { get; set; } = string.Empty;
        public string ParentType { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int RequiredCount { get; set; }
        public int OptionalCount { get; set; }
        public List<VaultAttachedFileDto> Files { get; set; } = new();
    }
}
