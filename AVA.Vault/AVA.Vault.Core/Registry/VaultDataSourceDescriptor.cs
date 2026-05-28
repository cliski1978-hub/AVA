using System;

namespace AVA.Vault.Core.Models
{
    /// <summary>
    /// Describes a physical or logical data source used by a Vault instance.
    /// Provides connection and access metadata for SQL, file, API, or in-memory vaults.
    /// </summary>
    public sealed class VaultDataSourceDescriptor
    {
        /// <summary>
        /// Unique identifier for the data source.
        /// Often derived from connection string, path, or remote endpoint hash.
        /// </summary>
        public string SourceId { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Human-readable display name for this source.
        /// </summary>
        public string DisplayName { get; set; } = "Local Vault Data Source";

        /// <summary>
        /// Describes the type of storage backend (e.g., Sqlite, Postgres, JsonFiles, Api, Memory).
        /// </summary>
        public VaultDataSourceKind Kind { get; set; } = VaultDataSourceKind.Sqlite;

        /// <summary>
        /// Connection string or path used to access this data source.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Optional API endpoint or remote node address, if this source is network-based.
        /// </summary>
        public string? EndpointUrl { get; set; }

        /// <summary>
        /// Whether this data source is currently available and reachable.
        /// </summary>
        public bool IsOnline { get; set; } = true;

        /// <summary>
        /// Timestamp when this data source was last verified.
        /// </summary>
        public DateTime LastChecked { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Optional identity of the node or service hosting the data source.
        /// (Useful for distributed or agentic systems.)
        /// </summary>
        public string? HostNodeId { get; set; }

        /// <summary>
        /// Returns a concise descriptor string for logs or debugging.
        /// </summary>
        public override string ToString()
        {
            var status = IsOnline ? "Online" : "Offline";
            return $"{DisplayName} [{Kind}] ({status}) ? {ConnectionString}";
        }
    }

    /// <summary>
    /// Enumeration of supported vault data source types.
    /// </summary>
    public enum VaultDataSourceKind
    {
        Sqlite,
        Postgres,
        JsonFiles,
        Api,
        Memory
    }
}
