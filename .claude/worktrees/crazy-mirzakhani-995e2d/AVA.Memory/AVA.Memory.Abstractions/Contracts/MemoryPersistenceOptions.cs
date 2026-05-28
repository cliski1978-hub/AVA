namespace AVA.Memory.Abstractions.Contracts
{
    /// <summary>
    /// Defines tunable parameters for persistence decision-making used by IPersistencePolicy.
    /// Shared between Core and API via configuration.
    /// </summary>
    public sealed class MemoryPersistenceOptions
    {
        /// <summary>
        /// Minimum salience required to persist a record to SQL or other durable stores.
        /// </summary>
        public double PersistThreshold { get; set; } = 0.55;

        /// <summary>
        /// Weight factors used by salience scoring logic (for future tuning).
        /// </summary>
        public double WeightNovelty { get; set; } = 0.5;
        public double WeightRecency { get; set; } = 0.3;
        public double WeightFrequency { get; set; } = 0.2;

        /// <summary>
        /// Tags that should always be persisted regardless of salience (e.g. "system", "identity").
        /// </summary>
        public string[] AlwaysPersistTags { get; set; } = new[] { "system", "core", "identity" };

        /// <summary>
        /// Tags that should never be persisted (e.g. "ephemeral", "scratch").
        /// </summary>
        public string[] NeverPersistTags { get; set; } = new[] { "ephemeral", "scratch" };
    }
}
