using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ava.Memory.Tests.Core.Utilities;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Tests.Core.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core
{
    /// <summary>
    /// Verifies FIFO, recall, and flush behavior of the working-memory component.
    /// Uses TestWorkingMemory from the Utilities folder.
    /// </summary>
    [TestFixture]
    internal class WorkingMemory_Tests
    {
        private TestWorkingMemory _working;

        [SetUp]
        public void SetUp()
        {
            _working = new TestWorkingMemory();
        }

        [Test]
        public async Task AddOrRefreshAsync_Should_Add_New_Record_And_Maintain_Size()
        {
            var rec1 = NewRecord("1");
            var rec2 = NewRecord("2");
            var rec3 = NewRecord("3");
            var rec4 = NewRecord("4");

            await _working.AddOrRefreshAsync(rec1, TimeSpan.FromSeconds(5), CancellationToken.None);
            await _working.AddOrRefreshAsync(rec2, TimeSpan.FromSeconds(5), CancellationToken.None);
            await _working.AddOrRefreshAsync(rec3, TimeSpan.FromSeconds(5), CancellationToken.None);
            await _working.AddOrRefreshAsync(rec4, TimeSpan.FromSeconds(5), CancellationToken.None);

            var items = await _working.GetItemsAsync(CancellationToken.None);

            items.Should().HaveCount(3, "capacity is 3");
            items.Select(r => r.ID).Should().NotContain("1", "the oldest item should be evicted");
        }

        [Test]
        public async Task AddOrRefreshAsync_Should_Refresh_Ttl_When_Existing()
        {
            var rec = NewRecord("A");
            await _working.AddOrRefreshAsync(rec, TimeSpan.FromSeconds(1), CancellationToken.None);
            await Task.Delay(200);

            await _working.AddOrRefreshAsync(rec, TimeSpan.FromSeconds(3), CancellationToken.None);

            var items = await _working.GetItemsAsync(CancellationToken.None);
            items.Should().ContainSingle(r => r.ID == "A");
        }

        [Test]
        public async Task GetItemsAsync_Should_Return_MostRecent_First()
        {
            await _working.AddOrRefreshAsync(NewRecord("A"), TimeSpan.FromSeconds(5), CancellationToken.None);
            await _working.AddOrRefreshAsync(NewRecord("B"), TimeSpan.FromSeconds(5), CancellationToken.None);
            await _working.AddOrRefreshAsync(NewRecord("C"), TimeSpan.FromSeconds(5), CancellationToken.None);

            var items = await _working.GetItemsAsync(CancellationToken.None);

            items.First().ID.Should().Be("C", "most recent record should be first");
            items.Last().ID.Should().Be("A");
        }

        [Test]
        public async Task FlushAsync_Should_Clear_All_Items()
        {
            await _working.AddOrRefreshAsync(NewRecord("1"), TimeSpan.FromSeconds(5), CancellationToken.None);
            await _working.AddOrRefreshAsync(NewRecord("2"), TimeSpan.FromSeconds(5), CancellationToken.None);

            await _working.FlushAsync(CancellationToken.None);

            var items = await _working.GetItemsAsync(CancellationToken.None);
            items.Should().BeEmpty();
        }

        [Test]
        public async Task Should_Handle_Concurrent_Adds_Safely()
        {
            var tasks = Enumerable.Range(0, 10)
                .Select(i => _working.AddOrRefreshAsync(NewRecord(i.ToString()), TimeSpan.FromSeconds(5), CancellationToken.None));

            await Task.WhenAll(tasks);

            var items = await _working.GetItemsAsync(CancellationToken.None);
            items.Should().NotBeNull();
            items.Count.Should().BeLessThanOrEqualTo(3, "capacity is bounded to 3");
        }

        private static MemoryRecordDto NewRecord(string id)
        {
            return new MemoryRecordDto
            {
                ID = id,
                Text = $"Record {id}",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow,
                Source = "test"
            };
        }
    }
}
