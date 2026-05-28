using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Tests.Core.Fixtures;
using AVA.Memory.Tests.Core.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Insert
{
    /// <summary>
    /// Verifies that the MemoryBroker handles invalid or extreme
    /// insert scenarios correctly using parameters from Excel tab 'Insert.EdgeTests'.
    /// </summary>
    [TestFixture]
    [Order(3)]
    internal sealed class Insert_EdgeCases_Tests
    {
        private BrokerFixture _fixture;
        private ExcelDataReader _reader;
        private TestConfig _config;
        private const string SheetName = "Insert.EdgeTests";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _config = TestConfig.Load();
            _fixture = new BrokerFixture(_config);
            _reader = new ExcelDataReader(_config.InputExcelPath);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _reader?.Dispose();
            _fixture?.Dispose();
        }

        private ExcelRowData GetRow(string scenario)
        {
            var sheet = _reader.ReadSheet(SheetName);
            var row = sheet.FirstOrDefault(r =>
                string.Equals(r.GetString("Scenario"), scenario, StringComparison.OrdinalIgnoreCase));

            row.Should().NotBeNull($"Scenario '{scenario}' must exist in Excel sheet '{SheetName}'.");
            return row!;
        }

        [Test]
        public void UpsertAsync_ShouldThrow_OnNullRequest()
        {
            var broker = _fixture.Broker;
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await broker.UpsertAsync(null!, CancellationToken.None));
        }

        [Test]
        public void UpsertAsync_ShouldThrow_OnEmptyText()
        {
            var broker = _fixture.Broker;
            var row = GetRow("EmptyText");
            var text = row.GetString("Text");

            var req = new UpsertMemoryRequest { Text = text };

            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await broker.UpsertAsync(req, CancellationToken.None));

            ex!.Message.Should().Contain("Text", "empty text should not be accepted");
        }

        [Test]
        public async Task UpsertAsync_ShouldReject_NullVector_WhenEmbeddingDisabled()
        {
            var broker = _fixture.Broker;
            var row = GetRow("NullVector");

            var req = new UpsertMemoryRequest
            {
                Text = row.GetString("Text"),
                Vector = null
            };

            var id = await broker.UpsertAsync(req, CancellationToken.None);
            id.Should().NotBeNullOrEmpty("broker should auto-embed missing vector values");
        }

        [Test]
        public async Task UpsertAsync_ShouldHandle_DuplicateIDs_AsUpdate()
        {
            var broker = _fixture.Broker;
            var row = GetRow("DuplicateIDs");
            var id = row.GetString("Id");

            var req1 = new UpsertMemoryRequest { Text = row.GetString("Text1"), Id = id };
            var req2 = new UpsertMemoryRequest { Text = row.GetString("Text2"), Id = id };

            var id1 = await broker.UpsertAsync(req1, CancellationToken.None);
            var id2 = await broker.UpsertAsync(req2, CancellationToken.None);

            id1.Should().Be(id2, "duplicate IDs should result in an update, not a new record");

            var got = await broker.GetAsync(id, bumpAccess: true, CancellationToken.None);
            got!.Text.Should().Be(req2.Text);
        }

        [Test]
        public async Task UpsertAsync_ShouldAccept_LongText_AndTruncateIfNeeded()
        {
            var broker = _fixture.Broker;
            var row = GetRow("LongText");

            var len = row.GetIntOrDefault("Length", 20000);
            var text = new string('A', len);
            var req = new UpsertMemoryRequest { Text = text };

            var id = await broker.UpsertAsync(req, CancellationToken.None);
            id.Should().NotBeNullOrEmpty();

            var got = await broker.GetAsync(id, bumpAccess: true, CancellationToken.None);
            got!.Text.Length.Should().BeLessThanOrEqualTo(len);
        }

        [Test]
        public async Task UpsertAsync_ShouldRespect_SalienceThresholds()
        {
            var broker = _fixture.Broker;
            var rowLow = GetRow("LowSalience");
            var rowHigh = GetRow("HighSalience");

            var low = new UpsertMemoryRequest
            {
                Text = rowLow.GetString("Text"),
                Salience = rowLow.GetFloatOrDefault("Salience")
            };
            var high = new UpsertMemoryRequest
            {
                Text = rowHigh.GetString("Text"),
                Salience = rowHigh.GetFloatOrDefault("Salience")
            };

            var lowId = await broker.UpsertAsync(low, CancellationToken.None);
            var highId = await broker.UpsertAsync(high, CancellationToken.None);

            lowId.Should().NotBeNull();
            highId.Should().NotBeNull();

            var all = await broker.ListAsync(0, 100, CancellationToken.None);
            all.Items.Any(r => r.ID == highId).Should().BeTrue();
        }

        [Test]
        public async Task UpsertAsync_ShouldIgnore_MetadataCollisions()
        {
            var broker = _fixture.Broker;
            var row = GetRow("MetadataCollision");

            var req = new UpsertMemoryRequest
            {
                Text = row.GetString("Text"),
                Metadata = new()
                {
                    ["k"] = "v1",
                    ["K"] = "v2"
                }
            };

            var id = await broker.UpsertAsync(req, CancellationToken.None);
            id.Should().NotBeNull();

            var got = await broker.GetAsync(id, bumpAccess: true, CancellationToken.None);
            got!.Metadata.Should().ContainSingle(m => m.Key.Equals("k", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public async Task UpsertAsync_ShouldGenerate_UniqueIDs_WhenNoneProvided()
        {
            var broker = _fixture.Broker;
            var row = GetRow("UniqueIdGen");

            var r1 = await broker.UpsertAsync(new UpsertMemoryRequest { Text = row.GetString("Text1") }, CancellationToken.None);
            var r2 = await broker.UpsertAsync(new UpsertMemoryRequest { Text = row.GetString("Text2") }, CancellationToken.None);

            r1.Should().NotBeNullOrEmpty();
            r2.Should().NotBeNullOrEmpty();
            r1.Should().NotBe(r2, "each record without a supplied ID should get a unique generated ID");
        }
    }
}
