using System;
using System.IO;
using AVA.Vault.Core.Config;

namespace AVA.Vault.Core
{
    /// <summary>
    /// Defines the default paths, configuration values, and behaviors
    /// used when initializing Vault instances across AVA systems.
    /// </summary>
    public static class VaultDefaults
    {
        // -------------------------------------------------------------
        // Standard folder and file names
        // -------------------------------------------------------------

        public static readonly string RootDirectory =
            Path.Combine(AppContext.BaseDirectory, "Vaults");

        public const string HeaderFileName = "vault.header.json";
        public const string NotesFolder = "notes";
        public const string GraphFolder = "graph";
        public const string MetadataFolder = "metadata";

        // -------------------------------------------------------------
        // Default configuration flags
        // -------------------------------------------------------------

        public static readonly bool DefaultEnableLogging = true;
        public static readonly bool DefaultAutoMigrate = true;

        // -------------------------------------------------------------
        // Default naming and display conventions
        // -------------------------------------------------------------

        public static string DefaultDisplayName => "Local Vault";
        public static string DefaultDatabaseFile => "vault_local.db";

        // -------------------------------------------------------------
        // Initialization helpers
        // -------------------------------------------------------------

        /// <summary>
        /// Ensures the default folder structure exists on disk.
        /// </summary>
        public static void EnsureDirectories(string vaultPath)
        {
            Directory.CreateDirectory(vaultPath);
            Directory.CreateDirectory(Path.Combine(vaultPath, NotesFolder));
            Directory.CreateDirectory(Path.Combine(vaultPath, GraphFolder));
            Directory.CreateDirectory(Path.Combine(vaultPath, MetadataFolder));
        }

        /// <summary>
        /// Creates a default VaultInstanceConfig when none is provided.
        /// </summary>
        public static VaultInstanceConfig CreateDefaultConfig(string? displayName = null)
        {
            var config = new VaultInstanceConfig
            {
                DisplayName = displayName ?? DefaultDisplayName,
                StoragePath = Path.Combine(RootDirectory, DefaultDatabaseFile),
                EnableLogging = DefaultEnableLogging,
                AutoMigrate = DefaultAutoMigrate
            };

            return config;
        }

        /// <summary>
        /// Returns the expected header file path for a given vault.
        /// </summary>
        public static string GetHeaderPath(string vaultDirectory)
            => Path.Combine(vaultDirectory, HeaderFileName);

        /// <summary>
        /// Returns the full absolute path for the default vault folder.
        /// </summary>
        public static string GetDefaultVaultDirectory(string displayName)
            => Path.Combine(RootDirectory, displayName.Replace(' ', '_'));
    }
}
