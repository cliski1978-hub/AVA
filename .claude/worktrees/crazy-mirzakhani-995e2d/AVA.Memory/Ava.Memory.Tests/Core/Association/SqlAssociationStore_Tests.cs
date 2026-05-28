using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Sql.Context;
using AVA.Memory.Sql.Stores;
using AVA.Memory.Tests.Core.Fixtures;
using AVA.Memory.Tests.Core.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Associations
{
    /// <summary>
    /// Validates SQL persistence and retrieval behavior of AssociationEdges through SqlAssociationStore.
    /// Ensures referenced MemoryRecords exist (IDs start after rec-100).
    /// Logs results to Excel for traceability.
    /// </summary>
    [TestFixture]
    internal class SqlAssociationStore_Tests
    {
        private TestContextFactory _contextFactory;
        private SqlAssociationStore _assocStore;
        private SqlMemoryStore _memStore;
        private MemoryDbContext _db;
        private TestConfig _config;
        private ExcelResultWriter _writer;
        private ExcelDataReader _reader;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _config = TestConfig.Load();

            var options = new DbContextOptionsBuilder<MemoryDbContext>()
                .UseSqlServer(_config.ConnectionString)
                .Options;

            _contextFactory = new TestContextFactory(options);
            _db = _contextFactory.CreateDbContext();
            _assocStore = new SqlAssociationStore(_contextFactory);
            _memStore = new SqlMemoryStore(_contextFactory);
            _writer = new ExcelResultWriter(_config.OutputExcelPath);
            _reader = new ExcelDataReader(_config.InputExcelPath);

            _db.Database.EnsureCreated();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _db?.Dispose();
        }

        /// <summary>
        /// Ensures that a test MemoryRecord and its associated vector components exist before inserting edges.
        /// </summary>
        private async Task EnsureMemoryRecordAsync(string id)
        {
            var existing = await _db.MemoryRecords.FirstOrDefaultAsync(r => r.ID == id);
            if (existing != null) return;

            var record = new MemoryRecordDto
            {
                ID = id,
                Text = $"Test Memory Record {id}",
                Vectors = new List<MemoryVectorDto>
                {
                    new MemoryVectorDto { Index = 0, Value = 0.1f, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MemoryVectorDto { Index = 1, Value = 0.2f, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
                    new MemoryVectorDto { Index = 2, Value = 0.3f, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
                },
                Metadata = new List<MemoryMetadataDto>
                {
                    new MemoryMetadataDto { Key = "source", Value = "SqlAssocTests" }
                },
                Tags = new List<MemoryTagDto>
                {
                    new MemoryTagDto { Tag = "unit-test" }
                },
                EpisodeId = "episode-1",
                ContextId = "context-1",
                Salience = 0.8,
                Novelty = 0.2,
                Frequency = 1,
                DecayRate = 0.01,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                Source = "TestHarness"
            };

            await _memStore.UpsertAsync(record, CancellationToken.None);
        }

        [Test]
        public async Task UpsertAsync_Should_Insert_New_Edge_And_LogResult()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();

            var fromId = "rec-101";
            var toId = "rec-102";

            await EnsureMemoryRecordAsync(fromId);
            await EnsureMemoryRecordAsync(toId);

            var edge = new AssociationEdgeDto
            {
                ID = Guid.NewGuid().ToString("N"),
                FromID = fromId,
                ToID = toId,
                Type = "linked_to",
                Weight = 0.9,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var id = await _assocStore.UpsertAsync(edge, CancellationToken.None);
            sw.Stop();

            var fetched = await _assocStore.GetAsync(id, CancellationToken.None);
            bool success = fetched != null;

            await _writer.AppendRowAsync("SqlAssoc.UpsertResults", new
            {
                RunId = runId,
                WhenUtc = whenUtc,
                EdgeID = edge.ID,
                SourceID = edge.FromID,
                TargetID = edge.ToID,
                RelationType = edge.Type,
                Weight = edge.Weight,
                Persisted = success,
                DurationMs = sw.ElapsedMilliseconds,
                Notes = success ? "Inserted OK" : "Insert failed"
            });

            success.Should().BeTrue();
            fetched!.FromID.Should().Be(edge.FromID);
            fetched.ToID.Should().Be(edge.ToID);
            fetched.Type.Should().Be(edge.Type);
        }

        [Test]
        public async Task GetAsync_Should_Return_Existing_Edge_And_Log()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;

            var fromId = "rec-103";
            var toId = "rec-104";

            await EnsureMemoryRecordAsync(fromId);
            await EnsureMemoryRecordAsync(toId);

            var edge = new AssociationEdgeDto
            {
                ID = Guid.NewGuid().ToString("N"),
                FromID = fromId,
                ToID = toId,
                Type = "causes",
                Weight = 0.7,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _assocStore.UpsertAsync(edge, CancellationToken.None);
            var sw = Stopwatch.StartNew();
            var got = await _assocStore.GetAsync(edge.ID, CancellationToken.None);
            sw.Stop();

            await _writer.AppendRowAsync("SqlAssoc.GetResults", new
            {
                RunId = runId,
                WhenUtc = whenUtc,
                EdgeID = edge.ID,
                SourceID = edge.FromID,
                TargetID = edge.ToID,
                RelationType = edge.Type,
                Weight = edge.Weight,
                Found = got != null,
                DurationMs = sw.ElapsedMilliseconds,
                Notes = got != null ? "Retrieved OK" : "Missing after insert"
            });

            got.Should().NotBeNull();
        }

        [Test]
        public async Task ListAsync_Should_Return_Paged_Results_And_Log()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;

            for (int i = 0; i < 3; i++)
            {
                var fromId = $"rec-{105 + i}";
                var toId = $"rec-{110 + i}";
                await EnsureMemoryRecordAsync(fromId);
                await EnsureMemoryRecordAsync(toId);

                var edge = new AssociationEdgeDto
                {
                    ID = Guid.NewGuid().ToString("N"),
                    FromID = fromId,
                    ToID = toId,
                    Type = "related_to",
                    Weight = 0.5 + i * 0.1,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _assocStore.UpsertAsync(edge, CancellationToken.None);
            }

            var sw = Stopwatch.StartNew();
            var (items, total) = await _assocStore.ListAsync(0, 10, CancellationToken.None);
            sw.Stop();

            await _writer.AppendRowAsync("SqlAssoc.ListResults", new
            {
                RunId = runId,
                WhenUtc = whenUtc,
                Count = items.Count,
                Total = total,
                DurationMs = sw.ElapsedMilliseconds,
                Notes = items.Any() ? "Returned results" : "Empty"
            });

            items.Should().NotBeEmpty();
            (total >= items.Count).Should().BeTrue();
        }

        [Test]
        public async Task DeleteAsync_Should_Remove_Edge_And_LogResult()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;

            var fromId = "rec-120";
            var toId = "rec-121";
            await EnsureMemoryRecordAsync(fromId);
            await EnsureMemoryRecordAsync(toId);

            var edge = new AssociationEdgeDto
            {
                ID = Guid.NewGuid().ToString("N"),
                FromID = fromId,
                ToID = toId,
                Type = "removes",
                Weight = 0.25,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _assocStore.UpsertAsync(edge, CancellationToken.None);

            var sw = Stopwatch.StartNew();
            var deleted = await _assocStore.DeleteAsync(edge.ID, CancellationToken.None);
            sw.Stop();

            var confirm = await _assocStore.GetAsync(edge.ID, CancellationToken.None);
            bool gone = confirm == null;

            await _writer.AppendRowAsync("SqlAssoc.DeleteResults", new
            {
                RunId = runId,
                WhenUtc = whenUtc,
                EdgeID = edge.ID,
                SourceID = edge.FromID,
                TargetID = edge.ToID,
                RelationType = edge.Type,
                Deleted = deleted,
                DurationMs = sw.ElapsedMilliseconds,
                Notes = gone ? "Deleted OK" : "Still exists"
            });

            deleted.Should().BeTrue();
            gone.Should().BeTrue();
        }
    }
}
