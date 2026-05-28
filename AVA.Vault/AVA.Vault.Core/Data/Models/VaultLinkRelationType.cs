namespace AVA.Vault.Core.Data.Models
{
    /// <summary>
    /// Defines the allowed relationship types for user-created VaultLinks.
    /// No AI-generated or graph-traversal links yet — user-created only.
    /// </summary>
    public static class VaultLinkRelationType
    {
        public const string References  = "References";
        public const string RelatedTo   = "RelatedTo";
        public const string DerivedFrom = "DerivedFrom";
        public const string ParentOf    = "ParentOf";
        public const string ChildOf     = "ChildOf";

        public static readonly IReadOnlyList<string> All = new[]
        {
            References,
            RelatedTo,
            DerivedFrom,
            ParentOf,
            ChildOf
        };

        public static bool IsValid(string relationType)
            => All.Contains(relationType);
    }
}
