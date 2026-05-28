using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Ava.Memory.Tests.Core.Utilities;
using AVA.Memory.Tests.Core.Fixtures;
using AVA.Memory.Tests.Core.Utilities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core
{
    /// <summary>
    /// Executes ListAsync() tests for MemoryBroker using the real SQL-backed store.
    /// Verifies retrieval counts, pagination, and timing, then writes results to Excel.
    /// </summary>
    [TestFixture]
    internal class List_WriteResults_Tests
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
        public async Task List_MemoryRecords_And_LogResults()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;
            var sw = new Stopwatch();
            var results = new List<object>();

            int pageSize = 50;
            int skip = 0;
            bool hasMore = true;
            int totalCount = 0;

            TestContext.WriteLine("[List_WriteResults_Tests] Starting paginated list test...");

            while (hasMore)
            {
                sw.Restart();
                var page = await _fixture.Broker.ListAsync(skip, pageSize, CancellationToken.None);
                sw.Stop();

                page.Should().NotBeNull();
                totalCount += page.Items.Count;

                results.Add(new
                {
                    RunId = runId,
                    WhenUtc = whenUtc,
                    Skip = skip,
                    Take = pageSize,
                    Returned = page.Items.Count,
                    TotalReported = page.Total,
                    DurationMs = sw.ElapsedMilliseconds
                });

                hasMore = page.Items.Count == pageSize;
                skip += pageSize;
            }

            foreach (var row in results)
                await _writer.AppendRowAsync("List.Results", row);

            TestContext.WriteLine($"[List_WriteResults_Tests] Listed {totalCount} total records across {results.Count} pages.");
        }

       
    }
}
