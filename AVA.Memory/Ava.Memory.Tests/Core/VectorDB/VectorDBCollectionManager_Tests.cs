using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;
using AVA.Memory.Core.Configuration;
using AVA.Memory.Core.Services;
using AVA.Memory.Core.Vector;   // ✅ added for InMemoryVectorDBDriver
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Vector
{
    /// <summary>
    /// Validates collection lifecycle management and synchronization logic
    /// for <see cref="VectorDBCollectionManager"/>.
    /// </summary>
    [TestFixture]
    public sealed class VectorDBCollectionManager_Tests
    {
        private VectorDBCollectionManager _manager;
        private InMemoryVectorDBDriver _driver;
        private VectorConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = new VectorConfig
            {
                ActiveDriver = "InMemory",
                Endpoint = "inmemory://localhost",
                DefaultCollection = "ava_test_memory",
                Dimension = 8,
                Metric = "cosine"
            };

            _driver = new InMemoryVectorDBDriver();
            _manager = new VectorDBCollectionManager(_driver, _config);
        }

        [Test]
        public async Task CreateIfNotExistsAsync_Should_Create_New_Collection()
        {
            var collection = new VectorDBCollectionDto
            {
                Name = "test_collection",
                Dimension = 8,
                Metric = "cosine"
            };

            var created = await _manager.CreateIfNotExistsAsync(collection, CancellationToken.None);

            created.Should().BeTrue();
            var collections = await _driver.ListCollectionsAsync();
            collections.Should().ContainSingle(c => c.Name == "test_collection");
        }

        [Test]
        public async Task CreateIfNotExistsAsync_Should_Not_Recreate_Existing_Collection()
        {
            var collection = new VectorDBCollectionDto
            {
                Name = "existing_collection",
                Dimension = 8
            };

            await _manager.CreateIfNotExistsAsync(collection, CancellationToken.None);
            var result = await _manager.CreateIfNotExistsAsync(collection, CancellationToken.None);

            result.Should().BeTrue();
            var all = await _driver.ListCollectionsAsync();
            all.Count.Should().Be(1);
        }

        [Test]
        public async Task ExistsAsync_Should_Return_True_When_Collection_Present()
        {
            await _manager.CreateIfNotExistsAsync(new VectorDBCollectionDto
            {
                Name = "my_collection"
            }, CancellationToken.None);

            var exists = await _manager.ExistsAsync("my_collection", CancellationToken.None);

            exists.Should().BeTrue();
        }

        [Test]
        public async Task ExistsAsync_Should_Return_False_When_Collection_Missing()
        {
            var exists = await _manager.ExistsAsync("unknown_collection", CancellationToken.None);
            exists.Should().BeFalse();
        }

        [Test]
        public async Task ListCollectionsAsync_Should_Return_All_Collections()
        {
            await _manager.CreateIfNotExistsAsync(new VectorDBCollectionDto { Name = "c1" }, CancellationToken.None);
            await _manager.CreateIfNotExistsAsync(new VectorDBCollectionDto { Name = "c2" }, CancellationToken.None);

            var list = await _manager.ListCollectionsAsync(CancellationToken.None);

            list.Should().HaveCount(2);
            list.Select(c => c.Name).Should().Contain(new[] { "c1", "c2" });
        }

        [Test]
        public async Task SyncAsync_Should_Log_Collection_Sync()
        {
            await _manager.CreateIfNotExistsAsync(new VectorDBCollectionDto { Name = "sync_test" }, CancellationToken.None);

            var result = await _manager.SyncAsync(CancellationToken.None);

            result.Should().BeTrue();
        }

        [Test]
        public async Task DeleteAsync_Should_Not_Throw_And_Return_True()
        {
            await _manager.CreateIfNotExistsAsync(new VectorDBCollectionDto { Name = "to_delete" }, CancellationToken.None);

            var deleted = await _manager.DeleteAsync("to_delete", CancellationToken.None);

            deleted.Should().BeTrue();
            var collections = await _driver.ListCollectionsAsync();
            collections.Any(c => c.Name == "to_delete").Should().BeFalse();
        }
    }
}
