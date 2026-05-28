using System;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Tests.Core.Fixtures;
using AVA.Memory.Tests.Core.Utilities;
using FluentAssertions;
using NUnit.Framework;
using Microsoft.EntityFrameworkCore;

namespace AVA.Memory.Tests.Core
{
    /// <summary>
    /// Loads all Excel-based test data (MemoryRecords + AssociationEdges)
    /// into the broker before running other tests.
    /// Ensures MemoryRecords are committed to SQL before inserting edges.
    /// </summary>
    [TestFixture, Ignore("Initialization complete — disable to prevent reseeding")]
    [Order(0)]
    internal sealed class ExcelData_Initializer_Tests
    {
        private BrokerFixture _fixture;
        private ExcelDataReader _reader;
        private ExcelResultWriter _writer;
        private TestConfig _config;

        [OneTimeSetUp]
        public void Setup()
        {
            _config = TestConfig.Load();
            _fixture = new BrokerFixture(_config);
            _reader = new ExcelDataReader(_config.InputExcelPath);
            _writer = new ExcelResultWriter(_config.OutputExcelPath);

            TestContext.WriteLine("[Initializer] Setup complete.");
        }

        [Test, Order(1)]
        public async Task Insert_All_MemoryRecords_FromExcel()
        {
            TestContext.WriteLine("[Initializer] Inserting MemoryRecords...");
            var rows = _reader.ReadSheet("Memory.Records");
            rows.Should().NotBeEmpty("Excel Memory.Records tab must contain test data.");

            int inserted = 0;

            foreach (var row in rows)
            {
                var request = new UpsertMemoryRequest
                {
                    Id = row.GetString("Id"),
                    Text = row.GetString("Text"),
                    Salience = row.GetFloatOrDefault("Salience"),
                    Novelty = row.GetFloatOrDefault("Novelty"),
                    Frequency = row.GetFloatOrDefault("Frequency"),
                    Tags = row.GetCsvArray("TagsCsv")
                };

                var id = await _fixture.Broker.UpsertAsync(request, CancellationToken.None);
                id.Should().NotBeNullOrEmpty($"Row {inserted + 1} failed to insert.");

                inserted++;

                await _writer.AppendRowAsync("Results", new
                {
                    Sheet = "Memory.Records",
                    RecordId = id,
                    Text = request.Text,
                    Salience = request.Salience,
                    Novelty = request.Novelty,
                    Frequency = request.Frequency,
                    Tags = string.Join(",", request.Tags ?? Array.Empty<string>()),
                    Status = "Inserted",
                    WhenUtc = DateTime.UtcNow
                });
            }

            // 🔹 Force EF to commit all MemoryRecords to SQL
            await _fixture.DbContext.SaveChangesAsync();
            var count = await _fixture.DbContext.MemoryRecords.CountAsync();
            TestContext.WriteLine($"[VERIFY] MemoryRecords in current context: {count}");

            try
            {
                var conn = _fixture.DbContext.Database.GetDbConnection();
                TestContext.WriteLine($"[VERIFY] Active Connection String: {conn.ConnectionString}");

                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM [MemoryRecords];";
                    var result = await cmd.ExecuteScalarAsync();
                    TestContext.WriteLine($"[VERIFY] MemoryRecords physically in SQL: {result}");
                }
                await conn.CloseAsync();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[VERIFY] Connection verification failed: {ex.Message}");
            }

            await _writer.AppendRowAsync("Results", new
            {
                Sheet = "Memory.Records",
                RecordId = "(commit)",
                Text = "(all MemoryRecords committed)",
                Status = "Committed",
                WhenUtc = DateTime.UtcNow
            });
        }

        [Test, Order(2)]
        public async Task Insert_All_AssociationEdges_FromExcel()
        {


            try
            {
                var conn = _fixture.DbContext.Database.GetDbConnection();
                TestContext.WriteLine($"[VERIFY] Active Connection String: {conn.ConnectionString}");

                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM [MemoryRecords];";
                    var result = await cmd.ExecuteScalarAsync();
                    TestContext.WriteLine($"[VERIFY] MemoryRecords physically in SQL: {result}");
                }
                await conn.CloseAsync();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[VERIFY] Connection verification failed: {ex.Message}");
            }

            TestContext.WriteLine("[Initializer] Inserting AssociationEdges...");
            var rows = _reader.ReadSheet("Assoc.Edges");
            if (rows.Count == 0)
            {
                Assert.Ignore("No Assoc.Edges rows found in Excel workbook.");
                return;
            }

            int inserted = 0;

            foreach (var row in rows)
            {
                var edge = new AssociationEdgeDto
                {
                    ID = row.GetString("EdgeId"),
                    FromID = row.GetString("FromId"),
                    ToID = row.GetString("ToId"),
                    Weight = row.GetFloatOrDefault("Weight"),
                    Type = row.GetString("Type")
                };

                if(edge == null)
                {
                    TestContext.WriteLine($"NULL");
                }
                TestContext.WriteLine($"[VERIFY] Tried: {edge.FromID}");

                await _fixture.Broker.UpsertEdgeAsync(edge, CancellationToken.None);
                inserted++;

                await _writer.AppendRowAsync("Results", new
                {
                    Sheet = "Assoc.Edges",
                    EdgeId = edge.ID,
                    FromId = edge.FromID,
                    ToId = edge.ToID,
                    Weight = edge.Weight,
                    Type = edge.Type,
                    Status = "Inserted",
                    WhenUtc = DateTime.UtcNow
                });
            }
        
            await _fixture.DbContext.SaveChangesAsync();
            var count = await _fixture.DbContext.MemoryRecords.CountAsync();
            TestContext.WriteLine($"[VERIFY] MemoryRecords in current context: {count}");

            try
            {
                var conn = _fixture.DbContext.Database.GetDbConnection();
                TestContext.WriteLine($"[VERIFY] Active Connection String: {conn.ConnectionString}");

                await conn.OpenAsync();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM [MemoryRecords];";
                    var result = await cmd.ExecuteScalarAsync();
                    TestContext.WriteLine($"[VERIFY] MemoryRecords physically in SQL: {result}");
                }
                await conn.CloseAsync();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"[VERIFY] Connection verification failed: {ex.Message}");
            }


            await _writer.AppendRowAsync("Results", new
            {
                Sheet = "Assoc.Edges",
                EdgeId = "(commit)",
                Status = "Committed",
                WhenUtc = DateTime.UtcNow
            });
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            TestContext.WriteLine("[Initializer] TearDown complete.");
            _fixture?.Dispose();
            _reader?.Dispose();
            _writer?.Dispose();
        }
    }
}
