using System;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Core.Policies;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Policy
{
    [TestFixture]
    internal class ProductionPersistencePolicy_Tests
    {
        private ProductionPersistencePolicy _policy;
        private MemoryPersistenceOptions _options;

        [SetUp]
        public void SetUp()
        {
            _policy = new ProductionPersistencePolicy();
            _options = new MemoryPersistenceOptions();
        }

        [Test]
        public void HighSalience_ShouldPersist_ToSql()
        {
            var record = new MemoryRecordDto
            {
                Salience = 0.9F,
                Novelty = 0.8F,
                Frequency = 0.4F,
                CreatedAt = DateTime.UtcNow
            };
            var request = new UpsertMemoryRequest { Text = "Test" };

            var targets = _policy.DecideTargets(record, request, _options);

            targets.Should().HaveFlag(StorageTargets.Sql);
        }

       
        [Test]
        public void SystemTag_ShouldForce_SqlPersistence()
        {
            var record = new MemoryRecordDto
            {
                Salience = 0.1F,
                Novelty = 0.1F,
                Frequency = 0.1F,
                Tags = new() { new MemoryTagDto { Tag = "system" } },
                CreatedAt = DateTime.UtcNow
            };
            var request = new UpsertMemoryRequest { Text = "System tag" };

            var targets = _policy.DecideTargets(record, request, _options);

            targets.Should().HaveFlag(StorageTargets.Sql);
        }

       
        
        [Test]
        public void Tagless_ShouldNotThrow()
        {
            var record = new MemoryRecordDto
            {
                Salience = 0.5F,
                Novelty = 0.4F,
                Frequency = 0.4F,
                Tags = null,
                CreatedAt = DateTime.UtcNow
            };
            var request = new UpsertMemoryRequest { Text = "Tagless" };

            Action act = () => _policy.DecideTargets(record, request, _options);

            act.Should().NotThrow();
        }
    }
}
