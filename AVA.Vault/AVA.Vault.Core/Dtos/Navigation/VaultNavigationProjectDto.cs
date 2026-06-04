namespace AVA.Vault.Core.Dtos.Navigation
{
    public class VaultNavigationProjectDto
    {
        public string ProjectID { get; set; } = string.Empty;
        public string VaultID { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsArchived { get; set; }
        public bool IsExpanded { get; set; }
        public int SortOrder { get; set; }
        public VaultNavigationGroupDto NotesGroup { get; set; } = new();
        public VaultNavigationGroupDto WorkflowsGroup { get; set; } = new();
        public VaultNavigationGroupDto SessionsGroup { get; set; } = new();
    }
}
