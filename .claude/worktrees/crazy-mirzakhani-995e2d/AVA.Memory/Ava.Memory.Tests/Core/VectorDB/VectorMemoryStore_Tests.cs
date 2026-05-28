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
using AVA.Memory.Core.Vector;   // ✅ Added for InMemoryVectorDBDriver
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Vector
{
    /// <summary>
    /// Validates persistence, search, and deletion behavior of the VectorMemoryStore
    /// using atomic MemoryVectorDto.Value fields (no embedded list vectors).
    /// </summary>
    [TestFixture]
    public sealed class VectorMemoryStore_Tests
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
        public async Task UpsertAsync_Should_Create_Record_In_Collection()
        {
            var record = new MemoryRecordDto
            {
                ID = "rec001",
                Text = "The fox jumps high",
                Vectors = Enumerable.Range(0, 8)
                    .Select(i => new MemoryVectorDto { Index = i, Value = (float)(i + 1) / 10f })
                    .ToList(),
                Tags = new List<MemoryTagDto> { new() { Tag = "animals" } },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // ✅ Ensure the target collection exists
            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto { Name = _config.DefaultCollection },
                CancellationToken.None);

            await _store.UpsertAsync(record);

            var allCollections = await _collections.ListCollectionsAsync(CancellationToken.None);
            allCollections.Should().Contain(c => c.Name == _config.DefaultCollection);
        }

        [Test]
        public async Task SearchAsync_Should_Return_Similar_Record()
        {
            var rec = new MemoryRecordDto
            {
                ID = "rec002",
                Text = "Vector similarity test",
                Vectors = Enumerable.Range(0, 8)
                    .Select(i => new MemoryVectorDto { Index = i, Value = 0.3f })
                    .ToList(),
                Tags = new List<MemoryTagDto> { new() { Tag = "unit_test" } },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto { Name = _config.DefaultCollection },
                CancellationToken.None);

            await _store.UpsertAsync(rec);

            var queryVector = rec.Vectors.OrderBy(v => v.Index).Select(v => v.Value).ToArray();
            var results = await _store.SearchAsync(queryVector, 5);

            results.Should().NotBeEmpty();
            results.First().Text.Should().Contain("Vector");
        }

        [Test]
        public async Task DeleteAsync_Should_Remove_Record()
        {
            var rec = new MemoryRecordDto
            {
                ID = "rec003",
                Text = "Delete test entry",
                Vectors = Enumerable.Range(0, 8)
                    .Select(i => new MemoryVectorDto { Index = i, Value = 0.5f })
                    .ToList(),
                Tags = new List<MemoryTagDto> { new() { Tag = "cleanup" } },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // ✅ Ensure the default collection exists before inserting
            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto
                {
                    Name = _config.DefaultCollection,
                    Dimension = _config.Dimension,
                    Metric = _config.Metric
                },
                CancellationToken.None);

            // ✅ Insert and then delete the record
            await _store.UpsertAsync(rec);
            await _store.DeleteAsync(rec.ID, rec.Tags[0].Tag);

            // ✅ Verify the record no longer exists in search results
            var query = Enumerable.Repeat(0.5f, 8).ToArray();
            var results = await _store.SearchAsync(query, 5);

            results.Should().NotBeNull();
            results.Any(r => r.ID == rec.ID).Should().BeFalse("record should be removed from vector memory");
        }


        [Test]
        public async Task ToMetadataDictionary_Should_Contain_Core_Fields()
        {
            var rec = new MemoryRecordDto
            {
                ID = "rec004",
                Text = "Metadata test",
                Source = "unit-test",
                Vectors = Enumerable.Range(0, 8)
                    .Select(i => new MemoryVectorDto { Index = i, Value = (float)(i + 1) / 8f })
                    .ToList(),
                Tags = new List<MemoryTagDto> { new() { Tag = "meta" } },
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto { Name = _config.DefaultCollection },
                CancellationToken.None);

            await _store.UpsertAsync(rec);

            var query = rec.Vectors.OrderBy(v => v.Index).Select(v => v.Value).ToArray();
            var results = await _store.SearchAsync(query, 1);

            var dto = results.First();
            dto.Metadata.Should().Contain(m => m.Key == "text");
            dto.Metadata.Should().Contain(m => m.Key == "source");
        }
    }
}
