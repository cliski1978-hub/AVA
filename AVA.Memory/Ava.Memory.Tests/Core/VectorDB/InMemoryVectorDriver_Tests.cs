using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Core.Vector;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Vector
{
    /// <summary>
    /// Unit tests validating the behavior of <see cref="InMemoryVectorDBDriver"/>.
    /// </summary>
    [TestFixture]
    public sealed class InMemoryVectorDBDriver_Tests
    {
        private InMemoryVectorDBDriver _driver;

        [SetUp]
        public void SetUp()
        {
            _driver = new InMemoryVectorDBDriver();
        }

        [TearDown]
        public void TearDown()
        {
            _driver = null!;
        }

        [Test]
        public async Task EnsureCollectionAsync_Should_Create_New_Collection()
        {
            var collection = new VectorDBCollectionDto { Name = "test" };

            var result = await _driver.EnsureCollectionAsync(collection);

            result.Should().BeTrue();
            var collections = await _driver.ListCollectionsAsync();
            collections.Should().ContainSingle(c => c.Name == "test");
        }

        [Test]
        public async Task ListCollectionsAsync_Should_Return_All_Collections()
        {
            await _driver.EnsureCollectionAsync(new VectorDBCollectionDto { Name = "c1" });
            await _driver.EnsureCollectionAsync(new VectorDBCollectionDto { Name = "c2" });

            var list = await _driver.ListCollectionsAsync();

            list.Should().HaveCount(2);
            list.Select(c => c.Name).Should().Contain(new[] { "c1", "c2" });
        }

        [Test]
        public async Task UpsertAsync_Should_Add_Record_To_Default_Collection()
        {
            var record = new VectorDBRecord
            {
                Id = Guid.NewGuid().ToString("N"),
                Collection = "default",
                Vector = new float[] { 0.5f, 0.8f },
                Metadata = new Dictionary<string, object> { ["text"] = "sample" }
            };

            await _driver.EnsureCollectionAsync(new VectorDBCollectionDto { Name = "default" });
            await _driver.UpsertAsync(record);

            var results = await _driver.SearchAsync(new float[] { 0.5f, 0.8f }, 1);
            results.Should().ContainSingle(r => r.Id == record.Id);
        }

        [Test]
        public async Task DeleteAsync_Should_Remove_Record_By_Id()
        {
            const string collection = "main";
            var id = Guid.NewGuid().ToString("N");
            await _driver.EnsureCollectionAsync(new VectorDBCollectionDto { Name = collection });
            await _driver.UpsertAsync(new VectorDBRecord { Id = id, Collection = collection, Vector = new[] { 0.1f, 0.2f } });

            await _driver.DeleteAsync(id, collection);
            var results = await _driver.SearchAsync(new float[] { 0.1f, 0.2f }, 10);

            results.Should().BeEmpty();
        }

        [Test]
        public async Task DeleteCollectionAsync_Should_Remove_Entire_Collection()
        {
            await _driver.EnsureCollectionAsync(new VectorDBCollectionDto { Name = "delete_me" });
            var before = await _driver.ListCollectionsAsync();
            before.Should().ContainSingle(c => c.Name == "delete_me");

            var success = await _driver.DeleteCollectionAsync("delete_me");

            success.Should().BeTrue();
            var after = await _driver.ListCollectionsAsync();
            after.Should().BeEmpty();
        }

        [Test]
        public async Task SearchAsync_Should_Return_TopK_Limited_Results()
        {
            const string collection = "search";
            await _driver.EnsureCollectionAsync(new VectorDBCollectionDto { Name = collection });

            for (int i = 0; i < 5; i++)
            {
                await _driver.UpsertAsync(new VectorDBRecord
                {
                    Id = $"r{i}",
                    Collection = collection,
                    Vector = new[] { (float)i, (float)(i + 1) },
                    Metadata = new Dictionary<string, object> { ["index"] = i }
                });
            }

            var results = await _driver.SearchAsync(new float[] { 1f, 2f }, topK: 3);

            results.Should().HaveCount(3);
            results.All(r => r.Id.StartsWith("r")).Should().BeTrue();
        }

        [Test]
        public void Dispose_Should_Not_Throw()
        {
            Action act = () => _driver = new InMemoryVectorDBDriver();
            act.Should().NotThrow();
        }
    }
}
