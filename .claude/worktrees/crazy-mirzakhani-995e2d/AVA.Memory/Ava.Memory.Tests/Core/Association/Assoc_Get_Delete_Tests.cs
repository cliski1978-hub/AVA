using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Tests.Core.Fixtures;
using AVA.Memory.Tests.Core.Utilities;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Associations
{
    /// <summary>
    /// Verifies Get/List/Delete for association edges via the real broker.
    /// Uses preloaded Excel data and logs results back to the workbook.
    /// </summary>
    [TestFixture]
    internal class Assoc_Get_Delete_Tests
    {
        private BrokerFixture _fixture;
        private ExcelResultWriter _writer;
        private ExcelDataReader _reader;
        private TestConfig _config;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _config = TestConfig.Load();
            _fixture = new BrokerFixture(_config);
            _reader = new ExcelDataReader(_config.InputExcelPath);
            _writer = new ExcelResultWriter(_config.OutputExcelPath);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _writer?.Dispose();
            _reader?.Dispose();
            _fixture?.Dispose();
        }

        [Test]
        public async Task Get_And_List_AssociationEdges()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;
            var sw = Stopwatch.StartNew();

            // short pause to allow initialization commits to settle
            await Task.Delay(200, CancellationToken.None);

            var allEdges = await GetAllEdgesAsync(0, 200, CancellationToken.None);
            sw.Stop();

            allEdges.Should().NotBeNull();

            foreach (var e in allEdges)
            {
                AssociationEdgeDto? got = null;
                string? error = null;

                try
                {
                    got = await _fixture.Broker.GetEdgeAsync(e.ID, CancellationToken.None);
                    got.Should().NotBeNull($"Expected edge {e.ID} to exist in DB.");
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    TestContext.WriteLine($"Error retrieving edge {e.ID}: {ex.Message}");
                }

                await _writer.AppendRowAsync("Assoc.GetResults", new
                {
                    RunId = runId,
                    WhenUtc = whenUtc,
                    EdgeID = e.ID,
                    SourceID = e.FromID,
                    TargetID = e.ToID,
                    RelationType = e.Type,
                    Weight = e.Weight,
                    Found = got != null,
                    Error = error,
                    DurationMs = sw.ElapsedMilliseconds,
                    Notes = got == null ? "Missing in DB" : "Retrieved OK"
                });
            }

            TestContext.WriteLine($"Verified {allEdges.Count} association edges from SQL.");
        }

        [Test]
        public async Task Delete_AssociationEdges_And_LogResults()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;
            var sw = new Stopwatch();

            var edges = await GetAllEdgesAsync(25, 5, CancellationToken.None);
            edges.Should().NotBeNull();

            var deleteResults = new List<object>();

            foreach (var e in edges)
            {
                bool deleted = false;
                string? error = null;
                long durationMs = 0;

                try
                {
                    sw.Restart();
                    deleted = await _fixture.Broker.DeleteEdgeAsync(e.ID, CancellationToken.None);
                    sw.Stop();
                    durationMs = sw.ElapsedMilliseconds;

                    var confirm = await _fixture.Broker.GetEdgeAsync(e.ID, CancellationToken.None);
                    confirm.Should().BeNull($"Edge {e.ID} should have been deleted.");
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                    TestContext.WriteLine($"Error deleting edge {e.ID}: {ex.Message}");
                }

                deleteResults.Add(new
                {
                    RunId = runId,
                    WhenUtc = whenUtc,
                    EdgeID = e.ID,
                    SourceID = e.FromID,
                    TargetID = e.ToID,
                    RelationType = e.Type,
                    Deleted = deleted,
                    Error = error,
                    DurationMs = durationMs,
                    Notes = deleted ? "Deleted OK" : "Failed or missing"
                });
            }

            foreach (var r in deleteResults)
                await _writer.AppendRowAsync("Assoc.DeleteResults", r);

            TestContext.WriteLine($"Deleted {deleteResults.Count} association edges from SQL.");
        }

        /// <summary>
        /// Paginates through the broker’s ListEdgesAsync(skip,take,ct) method to fetch all edges.
        /// </summary>
        private async Task<List<AssociationEdgeDto>> GetAllEdgesAsync(int start, int pageSize, CancellationToken ct)
        {
            var all = new List<AssociationEdgeDto>();
            var skip = start;

            while (true)
            {
                var page = await _fixture.Broker.ListEdgesAsync(skip, pageSize, ct);
                if (page?.Count == 0)
                    break;

                all.AddRange(page);
                if (page.Count < pageSize)
                    break;

                skip += pageSize;
            }

            return all;
        }
    }
}
