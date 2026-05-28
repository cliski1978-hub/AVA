using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Abstractions.VectorDB;
using AVA.Memory.Core.Configuration;
using AVA.Memory.Core.Services;
using AVA.Memory.Core.Vector;   // ✅ Use real in-memory driver
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Vector
{
    /// <summary>
    /// Verifies correct behavior of <see cref="VectorDBMaintenanceContext"/>,
    /// including maintenance scanning, error handling, and reporting.
    /// </summary>
    [TestFixture]
    public sealed class VectorDBMaintenanceContext_Tests
    {
        private VectorDBMaintenanceContext _context;
        private InMemoryVectorDBDriver _driver;
        private VectorDBCollectionManager _collections;
        private DummyRouter _router;
        private VectorConfig _config;

        [SetUp]
        public void SetUp()
        {
            _driver = new InMemoryVectorDBDriver();
            _router = new DummyRouter();

            _config = new VectorConfig
            {
                ActiveDriver = "InMemory",
                Endpoint = "local://",
                DefaultCollection = "ava_memory",
                Dimension = 8,
                Metric = "cosine"
            };

            _collections = new VectorDBCollectionManager(_driver, _config);
            _context = new VectorDBMaintenanceContext(_driver, _collections, _router, _config);
        }

        [Test]
        public async Task RunMaintenanceAsync_Should_Record_Stats_For_Each_Collection()
        {
            // ✅ Create a real collection in the in-memory driver
            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto { Name = "demo_collection", Dimension = 8, Metric = "cosine" },
                CancellationToken.None);

            await _context.RunMaintenanceAsync(CancellationToken.None);

            var report = _context.GetLastReport();
            report.Should().NotBeNull();
            report.Should().ContainKey("demo_collection");
            var stats = report["demo_collection"];
            stats.Success.Should().BeTrue();
            stats.ActionsTaken.Should().Contain("Verified registration");
        }

        [Test]
        public async Task RunMaintenanceAsync_Should_Handle_Empty_Collections_Gracefully()
        {
            // No collections created
            await _context.RunMaintenanceAsync(CancellationToken.None);

            var report = _context.GetLastReport();
            report.Should().BeEmpty();
        }

        [Test]
        public async Task RunMaintenanceAsync_Should_Record_Failures()
        {
            // Manually create a collection but then simulate a failure via dimension mismatch
            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto
                {
                    Name = "broken_collection",
                    Dimension = 0, // invalid dimension triggers failure
                    Metric = "cosine"
                },
                CancellationToken.None);

            await _context.RunMaintenanceAsync(CancellationToken.None);
            var report = _context.GetLastReport();

            report.Should().ContainKey("broken_collection");
            var entry = report["broken_collection"];
            entry.Success.Should().BeFalse();
            entry.Error.Should().Contain("dimension");
        }

        [Test]
        public async Task GetLastReport_Should_Return_Copy()
        {
            await _collections.CreateIfNotExistsAsync(
                new VectorDBCollectionDto { Name = "copy_test", Dimension = 8, Metric = "cosine" },
                CancellationToken.None);

            await _context.RunMaintenanceAsync(CancellationToken.None);

            var first = _context.GetLastReport();
            var second = _context.GetLastReport();

            first.Should().NotBeSameAs(second);
        }
    }

    #region --- Dummy Router ---

    internal sealed class DummyRouter : IVectorDBRouter
    {
        public string GetTargetCollection(VectorDBRecord record)
        {
            // Always route to the default collection for test purposes
            return "ava_memory";
        }

        public Task MoveRecordAsync(VectorDBRecord record, string destination, CancellationToken ct)
        {
            // No-op; movement logic not needed for unit testing
            return Task.CompletedTask;
        }
    }

    #endregion
}
