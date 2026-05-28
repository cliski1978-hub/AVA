using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;
using AVA.Memory.Core.Configuration;
using AVA.Memory.Core.Services;
using AVA.Memory.Core.Stores;
using AVA.Memory.Core.Vector;   // ✅ Needed for InMemoryVectorDBDriver
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Vector
{
    /// <summary>
    /// Validates integration behavior between VectorMemoryStore, VectorDBCollectionManager, and IVectorDBDriver.
    /// Uses atomic MemoryVectorDto.Value fields (not list-based) to construct test data.
    /// </summary>
    [TestFixture]
    public sealed class VectorDBIntegration_Tests
    {
        private InMemoryVectorDBDriver _driver;
        private VectorDBCollectionManager _collections;
        private VectorMemoryStore _store;
        private VectorConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new VectorConfig
            {
                ActiveDriver = "InMemory",
                Endpoint = "local://",
                DefaultCollection = "ava_memory",
                Dimension = 8,
                Metric = "cosine"
            };

            _driver = new InMemoryVectorDBDriver();
            _collections = new VectorDBCollectionManager(_driver, _config);
            _store = new VectorMemoryStore(_driver, _collections, _config);
        }

        [Test]
        public async Task Upsert_Then_Search_Should_Return_Record()
        {
            var record = new MemoryRecordDto
            {
                ID = "r001",
                Text = "Hello vector world",
                Vectors = Enumerable.Range(0, 8)
                    .Select(i => new MemoryVectorDto { Index = i, Value = (float)(i + 1) / 10f })
                    .ToList(),
                Tags = new List<MemoryTagDto> { new() { Tag = "demo" } },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // ✅ Ensure the default collection exists before upsert
            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto { Name = _config.DefaultCollection },
                CancellationToken.None);

            // ✅ Upsert will use default collection automatically (set in config)
            await _store.UpsertAsync(record);

            var query = record.Vectors.Select(v => v.Value).ToArray();
            var results = await _store.SearchAsync(query, 3);

            results.Should().NotBeEmpty();
            results.First().Text.Should().Contain("Hello");
        }


        [Test]
        public async Task DeleteAsync_Should_Remove_Record()
        {
            var rec = new MemoryRecordDto
            {
                ID = "r002",
                Text = "Delete me",
                Vectors = Enumerable.Range(0, 8)
                    .Select(i => new MemoryVectorDto { Index = i, Value = 0.5f })
                    .ToList(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // ✅ ensure collection exists and record goes to the default
            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto { Name = _config.DefaultCollection },
                CancellationToken.None);

            // ✅ upsert now automatically uses the configured default collection
            await _store.UpsertAsync(rec);

            await _store.DeleteAsync(rec.ID);

            var query = Enumerable.Repeat(0.5f, 8).ToArray();
            var results = await _store.SearchAsync(query, 5);

            results.Any(r => r.ID == rec.ID).Should().BeFalse();
        }


        [Test]
        public async Task CollectionManager_Should_Create_And_List_Collections()
        {
            var dto = new VectorDBCollectionDto
            {
                Name = "integration_test",
                Dimension = 8,
                Metric = "cosine"
            };

            var created = await _collections.CreateIfNotExistsAsync(dto, CancellationToken.None);
            created.Should().BeTrue();

            var all = await _collections.ListCollectionsAsync(CancellationToken.None);
            all.Any(c => c.Name == "integration_test").Should().BeTrue();
        }

        [Test]
        public async Task MaintenanceContext_Should_Run_Without_Errors()
        {
            var router = new DummyRouter();
            var ctx = new VectorDBMaintenanceContext(_driver, _collections, router, _config);

            await ctx.RunMaintenanceAsync(CancellationToken.None);

            var report = ctx.GetLastReport();
            report.Should().NotBeNull();
            report.Values.All(s => s.Success).Should().BeTrue();
        }

        #region --- Dummy router ---

        internal sealed class DummyRouter : IVectorDBRouter
        {
            public string GetTargetCollection(VectorDBRecord record)
                => "ava_memory";

            public Task MoveRecordAsync(VectorDBRecord record, string destination, CancellationToken ct)
                => Task.CompletedTask;
        }

        #endregion
    }
}
