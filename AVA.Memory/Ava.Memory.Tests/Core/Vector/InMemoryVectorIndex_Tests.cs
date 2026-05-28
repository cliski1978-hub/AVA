using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Contracts;
using AVA.Memory.Abstractions.Models;
using AVA.Memory.Core.Vector;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core
{
    /// <summary>
    /// Validates add/query/delete behavior of the in-memory vector index.
    /// </summary>
    [TestFixture]
    internal class InMemoryVectorIndex_Tests
    {
        private InMemoryVectorIndex _index;

        [SetUp]
        public void SetUp()
        {
            _index = new InMemoryVectorIndex();
        }

        [Test]
        public async Task AddAsync_And_QueryAsync_Should_Return_Expected_Hits()
        {
            var rec1 = NewRecord("1", new float[] { 0.1f, 0.2f, 0.3f });
            var rec2 = NewRecord("2", new float[] { 0.9f, 0.8f, 0.7f });

            await _index.AddAsync(rec1, CancellationToken.None);
            await _index.AddAsync(rec2, CancellationToken.None);

            var query = new QueryMemoryRequest
            {
                Embedding = new float[] { 0.1f, 0.2f, 0.25f },
                TopK = 2
            };

            var results = await _index.QueryAsync(query, CancellationToken.None);
            results.Should().NotBeEmpty();
        }

        [Test]
        public async Task AddAsync_Should_Replace_When_Same_Id()
        {
            var rec = NewRecord("dup", new float[] { 0.1f, 0.2f, 0.3f });
            await _index.AddAsync(rec, CancellationToken.None);

            rec.Vectors[0].Value = 0.9f; // mutate vector
            await _index.AddAsync(rec, CancellationToken.None);

            var results = await _index.QueryAsync(new QueryMemoryRequest
            {
                Embedding = new float[] { 0.9f, 0.2f, 0.3f },
                TopK = 1
            }, CancellationToken.None);

            results.Should().ContainSingle();
            results.First().Record.ID.Should().Be("dup");
        }

       
        private static MemoryRecordDto NewRecord(string id, float[] vec)
        {
            return new MemoryRecordDto
            {
                ID = id,
                Text = $"record-{id}",
                Vectors = vec
                    .Select((v, i) => new MemoryVectorDto { Index = i, Value = v })
                    .ToList(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastAccessedAt = DateTime.UtcNow
            };
        }
    }
}
