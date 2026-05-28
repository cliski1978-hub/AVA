using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AVA.Vault.Core.Config
{
    /// <summary>
    /// Defines configuration and runtime behavior for a Vault instance.
    /// Supports both local and distributed operation, with full adapter compatibility.
    /// Includes identity mode selection and identity database configuration.
    /// </summary>
    public sealed class VaultInstanceConfig
    {
        // ------------------------------------------------------------
        // Identity Mode (Embedded / LocalDatabase / Remote)
        // ------------------------------------------------------------

        /// <summary>
        /// Controls how the Identity system is initialized and accessed.
        /// Embedded       = IdentityDbContext stored locally alongside Vault.
        /// LocalDatabase  = IdentityDbContext using an external DB connection string.
        /// Remote         = Identity services accessed via remote HTTP/UPS provider.
        /// </summary>
        public IdentityMode IdentityMode { get; set; } = IdentityMode.Embedded;

        /// <summary>
        /// Optional connection string when IdentityMode == LocalDatabase.
        /// Not used for Embedded or Remote modes.
        /// </summary>
        public string? IdentityConnectionString { get; set; }


        // ------------------------------------------------------------
        // Identity Behavior
        // ------------------------------------------------------------

        /// <summary>
        /// When true, Vault validates incoming identity metadata (e.g. UPS headers).
        /// </summary>
        public bool EnableIdentityValidation { get; set; } = true;

        /// <summary>
        /// When true, Vault automatically stamps all write operations with
        /// the current identity metadata (IdentityStamp).
        /// </summary>
        public bool EnableIdentityStamping { get; set; } = true;

        /// <summary>
        /// File path for Identity DB when running in Embedded mode.
        /// Defaults to "identity.db" in the same directory as StoragePath.
        /// </summary>
        public string EmbeddedIdentityPath { get; set; } = "identity.db";

        /// <summary>
        /// Full resolved path for the embedded Identity database file.
        /// Computed automatically based on StoragePath and EmbeddedIdentityPath.
        /// </summary>
        public string EmbeddedIdentityFullPath
        {
            get
            {
                var dir = Path.GetDirectoryName(StoragePath);
                if (string.IsNullOrWhiteSpace(dir))
                    dir = ".";

                return Path.Combine(dir, EmbeddedIdentityPath);
            }
        }


        // ------------------------------------------------------------
        // Vault ID (internal local instance ID)
        // ------------------------------------------------------------

        /// <summary>
        /// Generates a semi-stable unique Vault ID derived from machine name,
        /// storage path, and timestamp. Used when caller does not supply a VaultID.
        /// </summary>
        private static string GenerateVaultId(string storagePath)
        {
            using var sha = SHA256.Create();
            var source = $"{Environment.MachineName}:{storagePath}:{DateTime.UtcNow.Ticks}";
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(source));
            return Convert.ToHexString(bytes)[..20].ToLower();
        }

        /// <summary>
        /// Unique identifier for this Vault instance. Required for distributed modes.
        /// </summary>
        public string VaultID { get; set; }


        // ------------------------------------------------------------
        // Core Vault Configuration
        // ------------------------------------------------------------

        /// <summary>
        /// SQL Server connection string for the Vault DbContext.
        /// When set, UseSqlServer is used instead of UseSqlite.
        /// </summary>
        public string? VaultConnectionString { get; set; }

        /// <summary>
        /// Local SQLite file path. Only used when VaultConnectionString is null.
        /// </summary>
        public string StoragePath { get; set; } = "vault_local.db";

        /// <summary>
        /// Human-readable display name for UIs or logs.
        /// </summary>
        public string DisplayName { get; set; } = "Local Vault";

        /// <summary>
        /// Enable or disable internal logging.
        /// </summary>
        public bool EnableLogging { get; set; } = true;

        /// <summary>
        /// Whether to auto-run EF Core migrations at startup (for Vault and Identity).
        /// </summary>
        public bool AutoMigrate { get; set; } = true;


        // ------------------------------------------------------------
        // Runtime Behavior & Integration
        // ------------------------------------------------------------

        /// <summary>
        /// When true, adapters and services skip network/DB operations and
        /// use mock/in-memory providers. Useful for development and tests.
        /// </summary>
        public bool MockMode { get; set; } = false;

        /// <summary>
        /// Base URL for MemoryCore service. Used by sync adapters.
        /// </summary>
        public string? MemoryEndpoint { get; set; } = "http://localhost:8082";

        /// <summary>
        /// Base URL for the distributed VaultRegistry service.
        /// </summary>
        public string? RegistryEndpoint { get; set; } = "http://localhost:8090";

        /// <summary>
        /// Timeout for registry network operations (in seconds).
        /// </summary>
        public int RegistryTimeoutSeconds { get; set; } = 10;


        // ------------------------------------------------------------
        // Constructors
        // ------------------------------------------------------------

        public VaultInstanceConfig()
        {
            VaultID = GenerateVaultId(StoragePath);
        }

        public VaultInstanceConfig(string vaultId, string storagePath, string displayName)
        {
            VaultID = string.IsNullOrWhiteSpace(vaultId) ? GenerateVaultId(storagePath) : vaultId;
            StoragePath = storagePath;
            DisplayName = displayName;
        }

        public VaultInstanceConfig(
            string vaultId,
            string storagePath,
            string displayName,
            string? memoryEndpoint,
            string? registryEndpoint,
            bool mockMode,
            int registryTimeoutSeconds = 10)
        {
            VaultID = string.IsNullOrWhiteSpace(vaultId) ? GenerateVaultId(storagePath) : vaultId;
            StoragePath = storagePath;
            DisplayName = displayName;
            MemoryEndpoint = memoryEndpoint ?? "http://localhost:8082";
            RegistryEndpoint = registryEndpoint ?? "http://localhost:8090";
            MockMode = mockMode;
            RegistryTimeoutSeconds = registryTimeoutSeconds;
        }


        // ------------------------------------------------------------
        // Display / ToString
        // ------------------------------------------------------------

        public override string ToString() => $"{DisplayName} [{VaultID}]";
    }


    // ------------------------------------------------------------
    // IdentityMode Enum
    // ------------------------------------------------------------
    public enum IdentityMode
    {
        /// <summary>
        /// Vault hosts its own IdentityDbContext using a local SQLite file.
        /// </summary>
        Embedded,

        /// <summary>
        /// Vault uses IdentityDbContext with an external DB connection string (SQL Server or SQLite).
        /// </summary>
        LocalDatabase,

        /// <summary>
        /// Vault delegates all identity resolution/validation to a remote service (HTTP/UPS).
        /// </summary>
        Remote
    }
}
