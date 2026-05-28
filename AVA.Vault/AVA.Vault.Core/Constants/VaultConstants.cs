namespace AVA.Vault.Core.Constants
{
    public static class VaultConstants
    {
        // System-Reserved Tags
        public const string Tag_Project = "Project";
        public const string Tag_Reflection = "Reflection";
        public const string Tag_MemoryTrace = "MemoryTrace";
        public const string Tag_System = "System";

        // Semantic Tag Types
        public const string Tag_Goal = "Goal";
        public const string Tag_Idea = "Idea";
        public const string Tag_Task = "Task";
        public const string Tag_Resolved = "Resolved";
        public const string Tag_Contradiction = "Contradiction";

        // Placeholder for potential link types (if we model them later)
        public static class LinkTypes
        {
            public const string Supports = "supports";
            public const string Contradicts = "contradicts";
            public const string Elaborates = "elaborates";
            public const string Blocks = "blocks";
        }
    }
}
