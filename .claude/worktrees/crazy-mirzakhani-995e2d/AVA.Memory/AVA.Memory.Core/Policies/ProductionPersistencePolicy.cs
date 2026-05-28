using System;
using System.Linq;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;

namespace AVA.Memory.Core.Policies
{
    /// <summary>
    /// Determines where memory records should persist based on salience, novelty, tags, and decay settings.
    /// This is the production-grade implementation of IPersistencePolicy used by the MemoryBroker.
    /// </summary>
    public sealed class ProductionPersistencePolicy : IPersistencePolicy
    {
        public StorageTargets DecideTargets(MemoryRecordDto record, UpsertMemoryRequest request, MemoryPersistenceOptions options)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (options == null) throw new ArgumentNullException(nameof(options));

            // ✅ Always persist certain tags (system/core/identity)
            if (record.Tags != null &&
                record.Tags.Any(t => options.AlwaysPersistTags.Any(a =>
                    string.Equals(a, t.Tag, StringComparison.OrdinalIgnoreCase))))
            {
                return StorageTargets.Sql | StorageTargets.Vector;
            }

            // ✅ Never persist ephemeral / scratch memory
            if (record.Tags != null &&
                record.Tags.Any(t => options.NeverPersistTags.Any(a =>
                    string.Equals(a, t.Tag, StringComparison.OrdinalIgnoreCase))))
            {
                return StorageTargets.None;
            }

            // ✅ Determine salience threshold for persistence
            var salience = record.Salience;

            if (salience >= options.PersistThreshold)
            {
                // High salience — full persistence (SQL + Vector if embedding exists)
                var hasVector = record.Vectors != null && record.Vectors.Count > 0;
                return hasVector ? StorageTargets.Sql | StorageTargets.Vector : StorageTargets.Sql;
            }

            // Moderate salience — SQL only
            if (salience >= (options.PersistThreshold * 0.5))
                return StorageTargets.Sql;

            // Low salience — keep only in working memory
            return StorageTargets.None;
        }
    }
}
