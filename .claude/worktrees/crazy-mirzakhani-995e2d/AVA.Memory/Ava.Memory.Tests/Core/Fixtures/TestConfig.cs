using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace AVA.Memory.Tests.Core.Fixtures
{
    /// <summary>
    /// Provides runtime configuration for all Core tests.
    /// Loads connection strings and file paths from environment variables,
    /// appsettings.Test.json, or sensible defaults.
    /// </summary>
    public sealed class TestConfig
    {
        public string ConnectionString { get; private set; }
        public string InputExcelPath { get; private set; }
        public string OutputExcelPath { get; private set; }
        public string RunId { get; private set; }

        private TestConfig(
            string connectionString,
            string inputExcelPath,
            string outputExcelPath,
            string runId)
        {
            ConnectionString = connectionString;
            InputExcelPath = inputExcelPath;
            OutputExcelPath = outputExcelPath;
            RunId = runId;
        }

        /// <summary>
        /// Loads configuration from environment variables or an appsettings file.
        /// Ensures paths are created and run ID is unique.
        /// </summary>
        public static TestConfig Load()
        {
            // Load from appsettings.Test.json if present
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.Test.json", optional: true)
                .AddEnvironmentVariables();

            var config = builder.Build();

            // Connection string resolution priority
            var conn = config["TestConnectionString"]
                       ?? Environment.GetEnvironmentVariable("AVA_MEMORY_TEST_DB")
                       ?? "Server=localhost;Database=AVA_Memory_Test;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";

            // Input/Output Excel paths
            var inputDir = Path.Combine(AppContext.BaseDirectory, "Core", "TestData");
            var outputDir = Path.Combine(AppContext.BaseDirectory, "Core", "TestOutput");

            Directory.CreateDirectory(inputDir);
            Directory.CreateDirectory(outputDir);

            var inputExcel = Path.Combine(inputDir, "AVA.Memory.Tests.Input.xlsx");
            var runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var outputExcel = Path.Combine(outputDir, $"MemoryCore.TestResults.{runId}.xlsx");

            TestContext.WriteLine($"[TestConfig] Connection: {conn}");
            TestContext.WriteLine($"[TestConfig] InputExcel: {inputExcel}");
            TestContext.WriteLine($"[TestConfig] OutputExcel: {outputExcel}");

            return new TestConfig(conn, inputExcel, outputExcel, runId);
        }
    }
}
