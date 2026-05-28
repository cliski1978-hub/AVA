using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;
using AVA.Memory.Core.Configuration;
using AVA.Memory.Core.Services;
using AVA.Memory.Core.Vector;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Vector
{
    /// <summary>
    /// Validates runtime behavior and analytics aggregation of
    /// <see cref="VectorDBAnalyticsService"/> using real in-memory components.
    /// </summary>
    [TestFixture]
    public sealed class VectorDBAnalyticsService_Tests
    {
        private VectorDBAnalyticsService _analytics;
        private VectorDBCollectionManager _collections;
        private InMemoryVectorDBDriver _driver;
        private VectorConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new VectorConfig
            {
                DefaultCollection = "ava_memory",
                Dimension = 8,
                Metric = "cosine",
                Endpoint = "local://",
                ActiveDriver = "InMemory"
            };

            _driver = new InMemoryVectorDBDriver();
            _collections = new VectorDBCollectionManager(_driver, _config);
            _analytics = new VectorDBAnalyticsService(_collections, _driver, _config);
        }

        [Test]
        public void RecordUpsert_Should_Increment_Counter()
        {
            _analytics.RecordUpsert("knowledge");
            _analytics.RecordUpsert("knowledge");
            _analytics.RecordUpsert("archive");

            var snapshot = _analytics.GetAnalyticsSnapshotAsync(CancellationToken.None).Result;

            var knowledge = snapshot.First(x => x.Collection == "knowledge");
            knowledge.UpsertCount.Should().Be(2);

            var archive = snapshot.First(x => x.Collection == "archive");
            archive.UpsertCount.Should().Be(1);
        }

        [Test]
        public void RecordQuery_Should_Increment_Counter()
        {
            _analytics.RecordQuery("knowledge");
            _analytics.RecordQuery("knowledge");
            _analytics.RecordQuery("demo");

            var snapshot = _analytics.GetAnalyticsSnapshotAsync(CancellationToken.None).Result;

            var knowledge = snapshot.First(x => x.Collection == "knowledge");
            knowledge.QueryCount.Should().Be(2);

            var demo = snapshot.First(x => x.Collection == "demo");
            demo.QueryCount.Should().Be(1);
        }

        [Test]
        public async Task GetAnalyticsSnapshotAsync_Should_Include_Collection_Metadata()
        {
            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto
                {
                    Name = "knowledge_base",
                    Dimension = 8,
                    Metric = "cosine",
                    VectorCount = 256,
                    IsInitialized = true,
                    LastUpdated = DateTime.UtcNow
                },
                CancellationToken.None);

            var list = await _analytics.GetAnalyticsSnapshotAsync(CancellationToken.None);
            list.Should().NotBeEmpty();

            var snap = list.First(x => x.Collection == "knowledge_base");
            snap.VectorCount.Should().BeGreaterThanOrEqualTo(0);
            snap.Metric.Should().Be("cosine");

        }

        [Test]
        public async Task Reset_Should_Clear_Counters()
        {
            _analytics.RecordUpsert("knowledge");
            _analytics.RecordQuery("knowledge");

            _analytics.Reset();

            var list = await _analytics.GetAnalyticsSnapshotAsync(CancellationToken.None);
            var knowledge = list.First(x => x.Collection == "knowledge");
            knowledge.UpsertCount.Should().Be(0);
            knowledge.QueryCount.Should().Be(0);
        }

        [Test]
        public void RecordUpsert_Should_Default_To_Configured_Collection_When_Null()
        {
            _analytics.RecordUpsert(null);

            var list = _analytics.GetAnalyticsSnapshotAsync(CancellationToken.None).Result;
            list.Should().Contain(x => x.Collection == "ava_memory");
        }
    }
}
