namespace AVA.Memory.Abstractions.Contracts
{
    public enum PersistenceDirective
    {
        Default,
        ForceSql,
        MemoryOnly
    }

    public sealed class UpsertMemoryRequest
    {
        public string? Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public string[]? Tags { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public string? EpisodeId { get; set; }
        public string? ContextId { get; set; }

        // Existing directive (app can force behavior)
        public PersistenceDirective Persistence { get; set; } = PersistenceDirective.Default;

        // NEW: explicit store targets (when set, overrides policy in Default mode)
        public StorageTargets? Targets { get; set; }

        // Direct vector + scoring fields
        public float[]? Vector { get; set; }
        public float Salience { get; set; } = 0.0f;
        public float Novelty { get; set; } = 0.0f;
        public double Frequency { get; set; } = 1;
        public float DecayRate { get; set; } = 0.0f;

        //Identity Fields
        public string? PrimaryIdentityId { get; set; }
        public string? PrimaryIdentityHandle { get; set; }
        public string? PrimaryIdentityType { get; set; }
        public byte[]? IdentityList { get; set; }
    }
}
