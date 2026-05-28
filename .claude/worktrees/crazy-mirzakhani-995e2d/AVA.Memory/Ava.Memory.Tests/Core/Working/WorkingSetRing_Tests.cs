using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using AVA.Memory.Core.WorkingMemory;
using AVA.Memory.Core.Models;

namespace AVA.Memory.Tests.Core.Working
{
    /// <summary>
    /// Tests for <see cref="WorkingSetRing"/> verifying capacity, eviction,
    /// expiry, and priority-based behavior.
    /// </summary>
    [TestFixture]
    internal sealed class WorkingSetRing_Tests
    {
        private WorkingSetRing _ring;

        [SetUp]
        public void Setup()
        {
            _ring = new WorkingSetRing(capacity: 3);
        }

        [Test]
        public void Add_ShouldRespectCapacity_AndEvictLowestPriority()
        {
            var low = NewItem("low", WorkingSetPriority.Low);
            var med = NewItem("med", WorkingSetPriority.Normal);
            var high = NewItem("high", WorkingSetPriority.High);
            var incoming = NewItem("new", WorkingSetPriority.Normal);

            _ring.Add(low);
            _ring.Add(med);
            _ring.Add(high);
            _ring.Add(incoming); // triggers eviction

            var items = _ring.GetRecent().ToList();
            items.Should().HaveCount(3);

            // lowest priority (low) should be gone
            items.Any(i => i.ID == "low").Should().BeFalse();
        }

        [Test]
        public void Add_ShouldNotEvict_CriticalPriorityItems()
        {
            var critical = NewItem("crit", WorkingSetPriority.Critical);
            var low = NewItem("low", WorkingSetPriority.Low);
            var newItem = NewItem("new", WorkingSetPriority.Normal);

            _ring.Add(critical);
            _ring.Add(low);
            _ring.Add(newItem); // should not evict critical

            var items = _ring.GetRecent().ToList();
            items.Should().Contain(i => i.ID == "crit");
        }

        [Test]
        public void PurgeExpired_ShouldRemoveExpiredItems()
        {
            var expired = NewItem("old", WorkingSetPriority.Low, ttlSeconds: -1);
            var valid = NewItem("new", WorkingSetPriority.Normal, ttlSeconds: 5);

            _ring.Add(expired);
            _ring.Add(valid);
            _ring.PurgeExpired();

            var items = _ring.GetRecent().ToList();

            items.Should().ContainSingle(i => i.ID == "new");
            items.Should().NotContain(i => i.ID == "old");
        }

        [Test]
        public void GetRecent_ShouldReturnNewestFirst()
        {
            _ring.Add(NewItem("A", WorkingSetPriority.Low));
            Thread.Sleep(10);
            _ring.Add(NewItem("B", WorkingSetPriority.Low));
            Thread.Sleep(10);
            _ring.Add(NewItem("C", WorkingSetPriority.Low));

            var recents = _ring.GetRecent().ToList();

            recents.First().ID.Should().Be("C");
            recents.Last().ID.Should().Be("A");
        }

        [Test]
        public void Constructor_ShouldThrow_OnInvalidCapacity()
        {
            Action act = () => new WorkingSetRing(0);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        private static WorkingSetItem NewItem(string id, WorkingSetPriority priority, int ttlSeconds = 10)
        {
            return new WorkingSetItem
            {
                ID = id,
                Priority = priority,
                InsertedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddSeconds(ttlSeconds)
            };
        }
    }
}
