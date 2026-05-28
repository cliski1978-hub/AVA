using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AVA.Memory.Abstractions.Models.VectorDB;
using AVA.Memory.Core.Vector;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Vector
{
    /// <summary>
    /// Tests for verifying correctness of QdrantVectorDBDriver’s REST behavior,
    /// serialization logic, and graceful handling of API responses.
    /// </summary>
    [TestFixture]
    public sealed class QdrantVectorDBDriver_Tests
    {
        private MockHttpHandler _httpHandler;
        private HttpClient _httpClient;
        private QdrantVectorDBDriver _driver;

        [SetUp]
        public void SetUp()
        {
            _httpHandler = new MockHttpHandler();
            _httpClient = new HttpClient(_httpHandler)
            {
                BaseAddress = new Uri("http://localhost:6333")
            };

            // Inject test HttpClient into Qdrant driver
            _driver = new QdrantVectorDBDriver("http://localhost:6333")
            {
                // force internal handler swap
            };
            typeof(QdrantVectorDBDriver)
                .GetField("_http", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                .SetValue(_driver, _httpClient);
        }

        [Test]
        public async Task EnsureCollectionAsync_Should_Create_When_Not_Exists()
        {
            // Arrange
            _httpHandler.EnqueueResponse(HttpStatusCode.NotFound, "{}"); // check existence
            _httpHandler.EnqueueResponse(HttpStatusCode.OK, "{}");       // creation success

            var dto = new VectorDBCollectionDto { Name = "test", Dimension = 8 };

            // Act
            var result = await _driver.EnsureCollectionAsync(dto);

            // Assert
            result.Should().BeTrue();
            dto.IsInitialized.Should().BeTrue();
        }

        [Test]
        public async Task ListCollectionsAsync_Should_Parse_Result_Correctly()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new
            {
                result = new[]
                {
                    new { name = "collection_a" },
                    new { name = "collection_b" }
                }
            });
            _httpHandler.EnqueueResponse(HttpStatusCode.OK, json);

            // Act
            var list = await _driver.ListCollectionsAsync();

            // Assert
            list.Should().HaveCount(2);
            list[0].Name.Should().Be("collection_a");
            list[1].Name.Should().Be("collection_b");
        }

        [Test]
        public async Task UpsertAsync_Should_Send_Correct_Request()
        {
            // Arrange
            var record = new VectorDBRecord
            {
                Id = "abc123",
                Vector = new float[] { 0.5f, 0.9f },
                Metadata = new Dictionary<string, object> { ["text"] = "hello" }
            };

            _httpHandler.EnqueueResponse(HttpStatusCode.OK, "{}");

            // Act
            await _driver.UpsertAsync(record);

            // Assert
            _httpHandler.LastRequest!.RequestUri!.ToString()
                .Should().Contain("/collections/ava_memory/points");
        }

        [Test]
        public async Task DeleteAsync_Should_Call_Qdrant_Endpoint()
        {
            // Arrange
            _httpHandler.EnqueueResponse(HttpStatusCode.OK, "{}");

            // Act
            await _driver.DeleteAsync("abc123", "ava_memory");

            // Assert
            _httpHandler.LastRequest!.RequestUri!.ToString()
                .Should().Contain("/collections/ava_memory/points/delete");
        }

        [Test]
        public async Task DeleteCollectionAsync_Should_Return_True_On_Success()
        {
            _httpHandler.EnqueueResponse(HttpStatusCode.OK, "{}");

            var result = await _driver.DeleteCollectionAsync("delete_me");

            result.Should().BeTrue();
            _httpHandler.LastRequest!.RequestUri!.AbsolutePath.Should().Contain("delete_me");
        }

        [Test]
        public async Task SearchAsync_Should_Parse_Results()
        {
            // Arrange
            var response = new
            {
                result = new[]
                {
                    new
                    {
                        id = "r1",
                        score = 0.99,
                        payload = new Dictionary<string, object> { ["text"] = "alpha" }
                    },
                    new
                    {
                        id = "r2",
                        score = 0.85,
                        payload = new Dictionary<string, object> { ["text"] = "beta" }
                    }
                }
            };
            _httpHandler.EnqueueResponse(HttpStatusCode.OK, JsonSerializer.Serialize(response));

            // Act
            var results = await _driver.SearchAsync(new float[] { 1f, 2f }, 2);

            // Assert
            results.Should().HaveCount(2);
            results[0].Id.Should().Be("r1");
            results[1].Metadata["text"].Should().Be("beta");
        }

        [Test]
        public void Dispose_Should_Not_Throw()
        {
            Action act = () => _driver.Dispose();
            act.Should().NotThrow();
        }
    }

    /// <summary>
    /// Mock HttpMessageHandler used to simulate Qdrant REST API responses.
    /// </summary>
    internal sealed class MockHttpHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        public HttpRequestMessage? LastRequest { get; private set; }

        public void EnqueueResponse(HttpStatusCode status, string content)
        {
            _responses.Enqueue(new HttpResponseMessage(status)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;

            if (_responses.Count > 0)
                return Task.FromResult(_responses.Dequeue());

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            });
        }
    }
}
