using System;

namespace AVA.Vault.Core.Dtos.Navigation
{
    public class VaultNavigationItemDto
    {
        public string ItemID { get; set; } = string.Empty;
        public string ItemType { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ParentID { get; set; }
        public string? ParentType { get; set; }
        public int SortOrder { get; set; }
        public bool IsPinned { get; set; }
        public bool IsTemplate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
