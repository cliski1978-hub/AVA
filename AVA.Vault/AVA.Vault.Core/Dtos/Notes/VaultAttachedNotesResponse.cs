using System.Collections.Generic;

namespace AVA.Vault.Core.Dtos.Notes
{
    public class VaultAttachedNotesResponse
    {
        public string ParentID { get; set; } = string.Empty;
        public string ParentType { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int RequiredCount { get; set; }
        public int OptionalCount { get; set; }
        public List<VaultAttachedNoteDto> Notes { get; set; } = new();
    }
}
