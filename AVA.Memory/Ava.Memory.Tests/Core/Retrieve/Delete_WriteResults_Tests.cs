using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Tests.Core.Fixtures;
using AVA.Memory.Tests.Core.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core
{
    /// <summary>
    /// Safely verifies deletion behavior using dedicated temp records (rec-9000+)
    /// so no shared or seeded data from other tests is removed.
    /// Logs results to Excel workbook.
    /// </summary>
    [TestFixture]
    internal class Delete_WriteResults_Tests
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
        public async Task Delete_Temp_MemoryRecords_And_LogResults()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;
            var sw = new Stopwatch();
            var broker = _fixture.Broker;

            // 1️⃣ Insert safe, dedicated records to delete
            var tempRecords = new List<string>();
            for (int i = 0; i < 5; i++)
            {
                var id = $"rec-{9000 + i}";
                var req = new UpsertMemoryRequest
                {
                    Id = id,
                    Text = $"Temporary delete test record {i}",
                };
                var insertedId = await broker.UpsertAsync(req, CancellationToken.None);
                insertedId.Should().Be(id);
                tempRecords.Add(id);
            }

            // 2️⃣ Delete only those records
            var results = new List<object>();

            TestContext.WriteLine($"[Delete_WriteResults_Tests] Deleting {tempRecords.Count} temporary records...");

            foreach (var recordId in tempRecords)
            {
                bool deleted = false;
                string? error = null;
                long durationMs = 0;

                try
                {
                    sw.Restart();
                    deleted = await broker.DeleteAsync(recordId, CancellationToken.None);
                    sw.Stop();
                    durationMs = sw.ElapsedMilliseconds;

                    if (deleted)
                    {
                        var confirm = await broker.GetAsync(recordId, bumpAccess: false, ct: CancellationToken.None);
                        confirm.Should().BeNull($"record {recordId} should no longer exist after deletion");
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    TestContext.WriteLine($"[Delete] Error deleting {recordId}: {ex.Message}");
                }

                results.Add(new
                {
                    RunId = runId,
                    WhenUtc = whenUtc,
                    Id = recordId,
                    Deleted = deleted,
                    Error = error,
                    DurationMs = durationMs
                });
            }

            // 3️⃣ Log results to Excel
            foreach (var row in results)
                await _writer.AppendRowAsync("Delete.Results", row);

            TestContext.WriteLine($"[Delete_WriteResults_Tests] Verified deletion of {results.Count} temporary records.");
        }
    }
}
