using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Core.Providers;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core
{
    /// <summary>
    /// Validates core DummyEmbeddingProvider behavior.
    /// Ensures dimension, range, and deterministic response characteristics.
    /// </summary>
    [TestFixture]
    internal class DummyEmbeddingProvider_Tests
    {
        [Test]
        public async Task EmbedAsync_ShouldReturn768DimVector_InRange()
        {
            // Arrange
            var provider = new DummyEmbeddingProvider();

            // Act
            var result = await provider.EmbedAsync("test prompt", CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(768, "the dummy provider should produce 768-dimensional vectors");
            result.All(v => v >= -1f && v <= 1f).Should().BeTrue("all values should be within [-1,1]");
        }

        [Test]
        public async Task EmbedAsync_ShouldBeDeterministic_ForSameInput()
        {
            // Arrange
            var provider = new DummyEmbeddingProvider();

            // Act
            var vectorA = await provider.EmbedAsync("same input", CancellationToken.None);
            var vectorB = await provider.EmbedAsync("same input", CancellationToken.None);

            // Assert
            vectorA.Should().NotBeNull();
            vectorB.Should().NotBeNull();
            vectorA.Length.Should().Be(vectorB.Length);
            vectorA.SequenceEqual(vectorB).Should().BeTrue("the dummy provider should return deterministic results for identical input");
        }

        [Test]
        public async Task EmbedAsync_ShouldDiffer_ForDifferentInputs()
        {
            // Arrange
            var provider = new DummyEmbeddingProvider();

            // Act
            var vectorA = await provider.EmbedAsync("apple", CancellationToken.None);
            var vectorB = await provider.EmbedAsync("orange", CancellationToken.None);

            // Assert
            vectorA.Should().NotBeNull();
            vectorB.Should().NotBeNull();
            vectorA.Should().NotEqual(vectorB, "different inputs should yield different embeddings");
        }

        [Test]
        public void EmbedAsync_ShouldThrow_ForEmptyInput()
        {
            // Arrange
            var provider = new DummyEmbeddingProvider();

            // Act & Assert
            Assert.ThrowsAsync<System.ArgumentException>(async () =>
                await provider.EmbedAsync(string.Empty, CancellationToken.None),
                "empty text should be rejected by the provider");
        }
    }
}
