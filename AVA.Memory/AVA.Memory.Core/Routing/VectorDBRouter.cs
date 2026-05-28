using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;
using AVA.Memory.Core.Configuration;

namespace AVA.Memory.Core.Routing
{
    /// <summary>
    /// Determines which VectorDB collection a record belongs to based on
    /// its topic, tags, or contextual metadata.
    /// Automatically creates and registers new collections if needed.
    /// </summary>
    public sealed class VectorDBRouter : IVectorDBRouter
    {
        private readonly IVectorDBCollectionManager _manager;
        private readonly VectorConfig _config;

        public VectorDBRouter(IVectorDBCollectionManager manager, VectorConfig config)
        {
            _manager = manager ?? throw new ArgumentNullException(nameof(manager));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _config.Validate();
        }

        /// <inheritdoc />
        public string GetTargetCollection(VectorDBRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));

            // 1. Topic-based routing
            if (record.Metadata != null && record.Metadata.TryGetValue("topic", out var topicObj))
            {
                var topic = topicObj?.ToString()?.Trim();
                if (!string.IsNullOrEmpty(topic))
                    return NormalizeName(topic);
            }

            // 2. Tag-based routing
            if (record.Tags != null && record.Tags.Length > 0)
            {
                var tagTopic = record.Tags.FirstOrDefault();
                if (!string.IsNullOrEmpty(tagTopic))
                    return NormalizeName(tagTopic);
            }

            // 3. Fallback to default
            return _config.DefaultCollection;
        }

        /// <inheritdoc />
        public async Task MoveRecordAsync(VectorDBRecord record, string newCollection, CancellationToken ct)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            if (string.IsNullOrWhiteSpace(newCollection))
                throw new ArgumentException("Target collection cannot be null or empty.", nameof(newCollection));

            var oldCollection = GetTargetCollection(record);
            if (oldCollection.Equals(newCollection, StringComparison.OrdinalIgnoreCase))
                return; // no move needed

            // Ensure the new collection exists
            await _manager.CreateIfNotExistsAsync(new VectorDBCollectionDto
            {
                Name = newCollection,
                Dimension = record.Vector?.Length ?? _config.Dimension,
                Metric = _config.Metric
            }, ct);

            if (_config.EnableLogging)
                Console.WriteLine($"[VectorDB] Record '{record.Id}' moved from '{oldCollection}' → '{newCollection}'.");
        }

        #region Helpers

        private static string NormalizeName(string raw)
        {
            var safe = new string(raw
                .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
                .ToArray());
            return safe.Length > 0 ? safe.ToLowerInvariant() : "unnamed_collection";
        }

        #endregion
    }
}
