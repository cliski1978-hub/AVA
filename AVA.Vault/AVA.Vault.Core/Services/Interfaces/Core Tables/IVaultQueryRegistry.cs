using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AVA.Vault.Core.Data.Models;
using AVA.Vault.Core.Interfaces;

namespace AVA.Vault.Core.Services.Interfaces
{
    /// <summary>
    /// Defines a registry of precompiled EF Core queries for high-frequency Vault operations.
    /// Provides methods for retrieving cached query delegates or executing them directly.
    /// </summary>
    public interface IVaultQueryRegistry
    {
        /// <summary>
        /// Returns a precompiled delegate for retrieving notes by tag.
        /// </summary>
        Func<IVaultDbContext, string, IAsyncEnumerable<VaultNote>> GetNotesByTagQuery();

        /// <summary>
        /// Returns a precompiled delegate for retrieving notes by project.
        /// </summary>
        Func<IVaultDbContext, string, IAsyncEnumerable<VaultNote>> GetNotesByProjectQuery();

        /// <summary>
        /// Executes a precompiled query directly by name (if found in registry).
        /// </summary>
        Task<object?> ExecutePrecompiledAsync(string name, Dictionary<string, object> parameters, CancellationToken ct = default);

        /// <summary>
        /// Returns a list of all registered precompiled query names.
        /// </summary>
        List<string> GetRegisteredQueries();
    }
}
