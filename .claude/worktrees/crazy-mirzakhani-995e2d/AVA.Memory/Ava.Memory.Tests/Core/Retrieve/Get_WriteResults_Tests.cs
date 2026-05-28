using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ava.Memory.Tests.Core.Utilities;
using AVA.Memory.Tests.Core.Fixtures;
using AVA.Memory.Tests.Core.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core
{
    /// <summary>
    /// Executes read (Get) operations against the real SQL-connected MemoryBroker.
    /// Each result is logged to the Excel workbook for verification and auditing.
    /// </summary>
    [TestFixture]
    internal class Get_WriteResults_Tests
    {
        private BrokerFixture _fixture;
        private ExcelResultWriter _writer;
        private TestConfig _config;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _config = TestConfig.Load();
            _fixture = new BrokerFixture(_config);
            _writer = new ExcelResultWriter(_config.OutputExcelPath);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _writer?.Dispose();
            _fixture?.Dispose();
        }

        [Test]
        public async Task Get_All_MemoryRecords_And_LogResults()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;
            var sw = new Stopwatch();

            // Retrieve a batch of records from SQL through MemoryBroker.ListAsync
            var list = await _fixture.Broker.ListAsync(0, 500, CancellationToken.None);
            list.Should().NotBeNull();
            list.Items.Should().NotBeEmpty("expected existing records from prior inserts");

            var results = new List<object>();

            TestContext.WriteLine($"[Get_WriteResults_Tests] Reading {list.Items.Count} records...");

            foreach (var record in list.Items)
            {
                bool found = false;
                string? error = null;
                int tagCount = 0;
                int metaCount = 0;
                int vectorDim = 0;
                long durationMs = 0;

                try
                {
                    sw.Restart();

                    // ✅ Updated for new GetAsync signature
                    var got = await _fixture.Broker.GetAsync(record.ID, bumpAccess: false, ct: CancellationToken.None);

                    sw.Stop();
                    durationMs = sw.ElapsedMilliseconds;

                    if (got != null)
                    {
                        found = true;
                        tagCount = got.Tags?.Count ?? 0;
                        metaCount = got.Metadata?.Count ?? 0;
                        vectorDim = got.Vectors?.Count ?? 0;

                        // Sanity checks
                        got.Text.Should().NotBeNullOrEmpty();
                        got.CreatedAt.Should().NotBe(default);
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    TestContext.WriteLine($"[Get] Error retrieving {record.ID}: {ex.Message}");
                }

                results.Add(new
                {
                    RunId = runId,
                    WhenUtc = whenUtc,
                    Id = record.ID,
                    Found = found,
                    TagCount = tagCount,
                    MetadataCount = metaCount,
                    VectorCount = vectorDim,
                    Error = error,
                    DurationMs = durationMs
                });
            }

            foreach (var row in results)
                await _writer.AppendRowAsync("Get.Results", row);

            TestContext.WriteLine($"[Get_WriteResults_Tests] Retrieved {results.Count} records total.");
        }
    }
}
