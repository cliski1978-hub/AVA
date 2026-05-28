using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Sql.Context;
using AVA.Memory.Sql.Stores;
using AVA.Memory.Tests.Core.Fixtures;
using AVA.Memory.Tests.Core.Utilities;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Abstractions.Contracts;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core
{
    /// <summary>
    /// Integration tests for SqlVectorIndex using the real SQL database.
    /// Verifies vector persistence, retrieval, and cosine similarity queries.
    /// </summary>
    [TestFixture]
    internal class SqlVectorIndex_Tests
    {
        private TestConfig _config;
        private TestContextFactory _contextFactory;
        private SqlVectorIndex _index;
        private ExcelResultWriter _writer;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _config = TestConfig.Load();

            var options = new DbContextOptionsBuilder<MemoryDbContext>()
                .UseSqlServer(_config.ConnectionString)
                .Options;

            _contextFactory = new TestContextFactory(options);
            _index = new SqlVectorIndex(_contextFactory);
            _writer = new ExcelResultWriter(_config.OutputExcelPath);

            using var ctx = _contextFactory.CreateDbContext();
            ctx.Database.EnsureCreated();
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _writer?.Dispose();
        }

        [Test]
        public async Task Add_And_Query_VectorRecord()
        {
            var runId = _config.RunId;
            var whenUtc = DateTime.UtcNow;
            var sw = new Stopwatch();

            var id = Guid.NewGuid().ToString("N");
            var vector = new float[] { 0.8f, 0.2f, 0.0f };

            var record = new MemoryRecordDto
            {
                ID = id,
                Text = "SQL vector record test",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Vectors = vector.Select((v, i) => new MemoryVectorDto { Index = i, Value = v }).ToList()
            };

            sw.Restart();
            await _index.AddAsync(record, CancellationToken.None);
            sw.Stop();

            var query = new QueryMemoryRequest
            {
                Embedding = new float[] { 0.9f, 0.1f, 0.0f },
                TopK = 5,
                MinScore = 0.0f
            };

            sw.Restart();
            var results = await _index.QueryAsync(query, CancellationToken.None);
            sw.Stop();

            results.Should().NotBeEmpty("vector should be retrievable by similarity query");

            var top = results.First();
            top.Record.ID.Should().Be(id);
            top.Score.Should().BeGreaterThan(0.5f);

            await _writer.AppendRowAsync("SqlVectorIndex.Results", new
            {
                RunId = runId,
                WhenUtc = whenUtc,
                Operation = "Add+Query",
                VectorId = id,
                Returned = results.Count,
                TopScore = top.Score,
                DurationMs = sw.ElapsedMilliseconds
            });
        }

        [Test]
        public async Task Add_ShouldReplaceExistingVector()
        {
            var id = Guid.NewGuid().ToString("N");

            var record1 = new MemoryRecordDto
            {
                ID = id,
                Text = "Vector v1",
                Vectors = new List<MemoryVectorDto>
                {
                    new() { Index = 0, Value = 1 },
                    new() { Index = 1, Value = 0 },
                    new() { Index = 2, Value = 0 }
                }
            };

            var record2 = new MemoryRecordDto
            {
                ID = id,
                Text = "Vector v2",
                Vectors = new List<MemoryVectorDto>
                {
                    new() { Index = 0, Value = 0 },
                    new() { Index = 1, Value = 1 },
                    new() { Index = 2, Value = 0 }
                }
            };

            await _index.AddAsync(record1, CancellationToken.None);
            await _index.AddAsync(record2, CancellationToken.None);

            var query = new QueryMemoryRequest
            {
                Embedding = new float[] { 0, 1, 0 },
                TopK = 1
            };

            var results = await _index.QueryAsync(query, CancellationToken.None);
            results.Should().ContainSingle(r => r.Record.ID == id);
            results.First().Score.Should().BeGreaterThan(0.8f, "re-add should replace the vector");
        }
    }
}
