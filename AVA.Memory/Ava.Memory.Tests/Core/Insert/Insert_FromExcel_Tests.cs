using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Core.Policies;
using AVA.Memory.Core.Services;
using AVA.Memory.Tests.Core.Fixtures;
using AVA.Memory.Tests.Core.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Insert
{
    /// <summary>
    /// Reads Insert.Tests sheet from Excel and verifies persistence policy behavior.
    /// All base MemoryRecords are assumed to be pre-seeded by ExcelData_Initializer_Tests.
    /// </summary>
    [TestFixture]
    [Order(1)]
    internal sealed class Insert_FromExcel_Tests
    {
        private BrokerFixture _fixture;
        private ExcelDataReader _reader;
        private ProductionPersistencePolicy _policy;
        private MemoryPersistenceOptions _options;
        private TestConfig _config;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _config = TestConfig.Load();
            _fixture = new BrokerFixture(_config);
            _reader = new ExcelDataReader(_config.InputExcelPath);
            _policy = new ProductionPersistencePolicy();
            _options = new MemoryPersistenceOptions();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _reader?.Dispose();
            _fixture?.Dispose();
        }

        [Test]
        public async Task Insert_PolicyBehavior_FromExcel_ShouldMatchExpectedTargets()
        {
            var sheet = "Insert.Tests";
            var rows = _reader.ReadSheet(sheet);
            rows.Should().NotBeEmpty($"Excel sheet '{sheet}' must exist and contain insert test data.");

            var broker = _fixture.Broker;
            int executed = 0;

            foreach (var row in rows)
            {
                var id = row.GetString("Id");
                var text = row.GetString("Text");
                var salience = row.GetFloatOrDefault("Salience");
                var novelty = row.GetFloatOrDefault("Novelty");
                var frequency = row.GetFloatOrDefault("Frequency");
                var expectedTargetsCsv = row.GetString("ExpectedTargets");
                var tagsCsv = row.GetString("TagsCsv");

                var expectedTargets = ParseTargets(expectedTargetsCsv);

                var record = new MemoryRecordDto
                {
                    ID = id,
                    Text = text,
                    Salience = salience,
                    Novelty = novelty,
                    Frequency = frequency,
                    Tags = tagsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Select(t => new MemoryTagDto { Tag = t }).ToList(),
                    CreatedAt = DateTime.UtcNow
                };

                var request = new UpsertMemoryRequest { Id = record.ID, Text = record.Text };

                var resultId = await broker.UpsertAsync(request, CancellationToken.None);
                resultId.Should().NotBeNullOrEmpty($"Record '{id}' should insert successfully.");

                var targets = _policy.DecideTargets(record, request, _options);

                // Assert using the expected target list
                foreach (var target in Enum.GetValues(typeof(StorageTargets)).Cast<StorageTargets>())
                {
                    if (expectedTargets.HasFlag(target))
                        targets.Should().HaveFlag(target, $"Expected {target} for record {id}");
                }

                executed++;
            }

            TestContext.WriteLine($"✅ Executed {executed} insert policy checks from Excel sheet '{sheet}'.");
        }

        private StorageTargets ParseTargets(string csv)
        {
            if (string.IsNullOrWhiteSpace(csv))
                return StorageTargets.None;

            StorageTargets result = StorageTargets.None;
            var items = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var item in items)
            {
                if (Enum.TryParse<StorageTargets>(item, true, out var target))
                    result |= target;
            }

            return result;
        }
    }
}
