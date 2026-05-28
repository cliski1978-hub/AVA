using System;
using System.Collections.Generic;
using Ava.Memory.Tests.Core.Utilities;
using AVA.Memory.Abstractions;
using AVA.Memory.Core.Services;
using AVA.Memory.Sql.Context;
using AVA.Memory.Sql.Stores;
using Microsoft.EntityFrameworkCore;

namespace AVA.Memory.Tests.Core.Fixtures
{
    /// <summary>
    /// Provides a real SQL-backed MemoryBroker instance for integration tests.
    /// Uses SQL stores and a lightweight in-memory working set for short-term memory.
    /// </summary>
    public sealed class BrokerFixture : IDisposable
    {
        public MemoryBroker Broker { get; }
        public MemoryDbContext DbContext { get; }

        private readonly TestContextFactory _contextFactory;

        public BrokerFixture(TestConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            var options = new DbContextOptionsBuilder<MemoryDbContext>()
                .UseSqlServer(config.ConnectionString)
                .Options;

            _contextFactory = new TestContextFactory(options);
            DbContext = _contextFactory.CreateDbContext();

            // Real persistent stores
            var memoryStore = new SqlMemoryStore(_contextFactory);
            var associationStore = new SqlAssociationStore(_contextFactory);
            var vectorIndex = new SqlVectorIndex(_contextFactory);

            // Mock providers for embeddings and working memory
            var embeddingProvider = new TestEmbeddingProvider();
            var workingMemory = new TestWorkingMemory();

            // Configure broker options (matching current class)
            var brokerOptions = new MemoryBrokerOptions
            {
                PersistThreshold = 0.55,
                WeightNovelty = 0.5,
                WeightRecency = 0.3,
                WeightFrequency = 0.2,
                RecencyHalfLifeSeconds = 3600,
                MinWorkingTtlSeconds = 30,
                MaxWorkingTtlSeconds = 300,
                ReinforceTopHits = true,
                ReinforceTopN = 5,
                ReinforceTtlSeconds = 120
            };

            // Construct broker with correct argument order
            Broker = new MemoryBroker(
                new List<IMemoryStore> { memoryStore },
                vectorIndex,
                embeddingProvider,
                workingMemory,
                associationStore,
                brokerOptions
            );

            DbContext.Database.EnsureCreated();
        }

        public void Dispose()
        {
            DbContext?.Dispose();
        }
    }
}
