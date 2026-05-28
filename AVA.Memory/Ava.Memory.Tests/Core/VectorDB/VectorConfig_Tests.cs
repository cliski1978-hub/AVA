using System;
using System.IO;
using AVA.Memory.Core.Configuration;
using FluentAssertions;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Configuration
{
    /// <summary>
    /// Verifies correct behavior and integrity of <see cref="VectorConfig"/>,
    /// including defaults, constructors, and validation rules.
    /// </summary>
    [TestFixture]
    public sealed class VectorConfig_Tests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }

        [Test]
        public void DefaultConstructor_Should_Set_Sensible_Defaults()
        {
            var cfg = new VectorConfig();

            cfg.ActiveDriver.Should().Be("Qdrant");
            cfg.Endpoint.Should().Be("http://localhost:6333");
            cfg.DefaultCollection.Should().Be("ava_memory");
            cfg.Dimension.Should().BeGreaterThan(0);
            cfg.Metric.Should().Be("cosine");
            cfg.TimeoutMs.Should().Be(10000);
            cfg.EnableMaintenance.Should().BeTrue();
        }

        [Test]
        public void CustomConstructor_Should_Assign_All_Values()
        {
            var path = Path.Combine(_tempDir, "collections.json");
            var cfg = new VectorConfig(
                driver: "Milvus",
                endpoint: "http://localhost:19530",
                dimension: 512,
                metric: "euclidean",
                defaultCollection: "test_collection",
                apiKey: "test-key",
                registryPath: path
            );

            cfg.ActiveDriver.Should().Be("Milvus");
            cfg.Endpoint.Should().Be("http://localhost:19530");
            cfg.Dimension.Should().Be(512);
            cfg.Metric.Should().Be("euclidean");
            cfg.DefaultCollection.Should().Be("test_collection");
            cfg.ApiKey.Should().Be("test-key");
            cfg.RegistryPath.Should().Be(path);
        }

        [Test]
        public void Validate_Should_Create_Missing_Registry_Directory()
        {
            var registryPath = Path.Combine(_tempDir, "nested", "collections.json");
            var cfg = new VectorConfig
            {
                RegistryPath = registryPath,
                Endpoint = "http://localhost:6333",
                Dimension = 256,
                Metric = "cosine",
                DefaultCollection = "ava_memory"
            };

            // Directory does not exist yet
            Directory.Exists(Path.GetDirectoryName(registryPath)).Should().BeFalse();

            cfg.Validate();

            Directory.Exists(Path.GetDirectoryName(registryPath)).Should().BeTrue();
        }

        [Test]
        public void Validate_Should_Throw_On_Invalid_Endpoint()
        {
            var cfg = new VectorConfig
            {
                Endpoint = "",
                Dimension = 256,
                Metric = "cosine",
                DefaultCollection = "ava_memory",
                RegistryPath = Path.Combine(_tempDir, "collections.json")
            };

            Action act = () => cfg.Validate();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Endpoint*");
        }

        [Test]
        public void Validate_Should_Throw_On_Invalid_Dimension()
        {
            var cfg = new VectorConfig
            {
                Endpoint = "http://localhost:6333",
                Dimension = 0,
                Metric = "cosine",
                DefaultCollection = "ava_memory",
                RegistryPath = Path.Combine(_tempDir, "collections.json")
            };

            Action act = () => cfg.Validate();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Dimension*");
        }

        [Test]
        public void Validate_Should_Throw_On_Missing_Metric()
        {
            var cfg = new VectorConfig
            {
                Endpoint = "http://localhost:6333",
                Dimension = 128,
                Metric = "",
                DefaultCollection = "ava_memory",
                RegistryPath = Path.Combine(_tempDir, "collections.json")
            };

            Action act = () => cfg.Validate();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*Metric*");
        }

        [Test]
        public void ToString_Should_Return_Concise_Summary()
        {
            var cfg = new VectorConfig
            {
                ActiveDriver = "Qdrant",
                Endpoint = "http://localhost:6333",
                DefaultCollection = "ava_memory",
                Dimension = 768,
                Metric = "cosine"
            };

            var summary = cfg.ToString();
            summary.Should().Contain("Qdrant");
            summary.Should().Contain("ava_memory");
            summary.Should().Contain("Dim=768");
        }
    }
}
