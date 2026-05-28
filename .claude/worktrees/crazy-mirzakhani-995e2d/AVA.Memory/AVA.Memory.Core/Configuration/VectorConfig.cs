using System;
using System.IO;

namespace AVA.Memory.Core.Configuration
{
    /// <summary>
    /// Defines runtime configuration for the VectorDB layer.
    /// Provides connection parameters, default collection metadata,
    /// and registry persistence paths used during driver initialization.
    /// </summary>
    [Serializable]
    public sealed class VectorConfig
    {
        #region Properties

        /// <summary>
        /// The active driver identifier (e.g. "Qdrant", "Milvus", "InMemory").
        /// </summary>
        public string ActiveDriver { get; set; }

        /// <summary>
        /// Base URL or connection endpoint of the VectorDB backend service.
        /// </summary>
        public string Endpoint { get; set; }

        /// <summary>
        /// Optional API key used for authentication with the backend.
        /// </summary>
        public string ApiKey { get; set; }

        /// <summary>
        /// Default collection name used when none is provided by context.
        /// </summary>
        public string DefaultCollection { get; set; }

        /// <summary>
        /// Default vector dimensionality for all new collections.
        /// </summary>
        public int Dimension { get; set; }

        /// <summary>
        /// Default distance metric (e.g. "cosine", "euclidean", "dot").
        /// </summary>
        public string Metric { get; set; }

        /// <summary>
        /// Optional timeout in milliseconds for backend requests.
        /// </summary>
        public int TimeoutMs { get; set; } = 10000;

        /// <summary>
        /// Local path to the persistent collection registry file (e.g. "Data/collections.json").
        /// </summary>
        public string RegistryPath { get; set; }

        /// <summary>
        /// Enables background maintenance and collection-sync tasks if true.
        /// </summary>
        public bool EnableMaintenance { get; set; } = true;

        /// <summary>
        /// Enables verbose logging for VectorDB driver operations.
        /// </summary>
        public bool EnableLogging { get; set; } = false;

        // --------------------------------------------------------------------
        // 🔧 New maintenance tuning parameters
        // --------------------------------------------------------------------

        /// <summary>
        /// Maximum age (in days) before a record is considered stale and may be pruned.
        /// Used by VectorDBMaintenanceContext.
        /// </summary>
        public int MaxRecordAgeDays { get; set; } = 30;

        /// <summary>
        /// Minimum decay threshold below which records are eligible for pruning.
        /// Used by VectorDBMaintenanceContext.
        /// </summary>
        public double DecayThreshold { get; set; } = 0.1;

        #endregion

        #region Constructors

        public VectorConfig()
        {
            ActiveDriver = "Qdrant";
            Endpoint = "http://localhost:6333";
            DefaultCollection = "ava_memory";
            Dimension = 768;
            Metric = "cosine";
            RegistryPath = Path.Combine(AppContext.BaseDirectory, "Data", "collections.json");
        }

        public VectorConfig(
            string driver,
            string endpoint,
            int dimension = 768,
            string metric = "cosine",
            string defaultCollection = "ava_memory",
            string apiKey = null,
            string registryPath = null)
        {
            ActiveDriver = driver ?? "Qdrant";
            Endpoint = endpoint?.TrimEnd('/') ?? "http://localhost:6333";
            Dimension = dimension;
            Metric = metric ?? "cosine";
            DefaultCollection = defaultCollection ?? "ava_memory";
            ApiKey = apiKey;
            RegistryPath = registryPath ?? Path.Combine(AppContext.BaseDirectory, "Data", "collections.json");
        }

        #endregion

        #region Methods

        /// <summary>
        /// Validates the configuration for completeness before initialization.
        /// Throws exceptions for missing or invalid values.
        /// </summary>
        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(Endpoint))
                throw new InvalidOperationException("VectorConfig.Endpoint cannot be null or empty.");

            if (Dimension <= 0)
                throw new InvalidOperationException("VectorConfig.Dimension must be greater than zero.");

            if (string.IsNullOrWhiteSpace(Metric))
                throw new InvalidOperationException("VectorConfig.Metric cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(DefaultCollection))
                throw new InvalidOperationException("VectorConfig.DefaultCollection cannot be null or empty.");

            if (string.IsNullOrWhiteSpace(RegistryPath))
                throw new InvalidOperationException("VectorConfig.RegistryPath cannot be null or empty.");

            var dir = Path.GetDirectoryName(RegistryPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        public override string ToString()
        {
            return $"{ActiveDriver} | {Endpoint} | Default={DefaultCollection} | Dim={Dimension} | Metric={Metric}";
        }

        #endregion
    }
}
